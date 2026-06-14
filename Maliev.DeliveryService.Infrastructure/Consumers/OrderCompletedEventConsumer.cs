using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Delivery;
using Maliev.MessagingContracts.Contracts.Orders;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maliev.DeliveryService.Infrastructure.Consumers;

/// <summary>
/// Consumes OrderCompletedEvent to auto-create delivery note drafts
/// </summary>
public class OrderCompletedEventConsumer : IConsumer<OrderCompletedEvent>
{
    private readonly IDeliveryNoteService _deliveryNoteService;
    private readonly IOrderServiceClient _orderServiceClient;
    private readonly DeliveryDbContext _context;
    private readonly ILogger<OrderCompletedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of OrderCompletedEventConsumer.
    /// </summary>
    public OrderCompletedEventConsumer(
        IDeliveryNoteService deliveryNoteService,
        IOrderServiceClient orderServiceClient,
        DeliveryDbContext context,
        ILogger<OrderCompletedEventConsumer> logger)
    {
        _deliveryNoteService = deliveryNoteService;
        _orderServiceClient = orderServiceClient;
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var orderEvent = context.Message;

        _logger.LogInformation(
            "Received OrderCompletedEvent: OrderId={OrderId}, OrderNumber={OrderNumber}, CompletedAt={CompletedAt}",
            orderEvent.Payload.OrderId, orderEvent.Payload.OrderNumber, orderEvent.Payload.CompletedAt);

        if (!IsRoutedToDeliveryService(orderEvent))
        {
            _logger.LogDebug(
                "Skipping OrderCompletedEvent not routed to DeliveryService. OrderId={OrderId}, OrderNumber={OrderNumber}",
                orderEvent.Payload.OrderId,
                orderEvent.Payload.OrderNumber);
            return;
        }

        if (!orderEvent.Payload.JobSucceeded)
        {
            _logger.LogWarning(
                "Skipping delivery note auto-creation for failed order completion. OrderId={OrderId}, OrderNumber={OrderNumber}",
                orderEvent.Payload.OrderId,
                orderEvent.Payload.OrderNumber);
            return;
        }

        // Idempotency check: prevent duplicate delivery notes for the same order.
        // Older/manual flows may store the stable order GUID, while automatic flows store the order number.
        var orderId = orderEvent.Payload.OrderId.ToString("D");
        var existingDeliveryNote = await _context.DeliveryNotes
            .Where(dn => !dn.IsDeleted
                && (dn.OrderId == orderEvent.Payload.OrderNumber || dn.OrderId == orderId))
            .FirstOrDefaultAsync(context.CancellationToken);

        if (existingDeliveryNote != null)
        {
            _logger.LogWarning(
                "Delivery note already exists for OrderId={OrderId}, OrderNumber={OrderNumber}, skipping auto-creation. Existing DeliveryNoteId={DeliveryNoteId}",
                orderEvent.Payload.OrderId, orderEvent.Payload.OrderNumber, existingDeliveryNote.DeliveryNoteId);
            return;
        }

        try
        {
            var orderDetails = await _orderServiceClient.GetOrderAsync(
                orderEvent.Payload.OrderNumber,
                context.CancellationToken);
            var deliveryItems = orderDetails?.Items.Count > 0
                ? orderDetails.Items.Select(item => new CreateDeliveryNoteItemRequest
                {
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    QuantityOrdered = item.QuantityOrdered,
                    QuantityManufactured = item.QuantityManufactured,
                    QuantityDelivered = item.QuantityManufactured,
                    UnitOfMeasure = item.UnitOfMeasure
                }).ToList()
                : orderEvent.Payload.Items.Select(item => new CreateDeliveryNoteItemRequest
                {
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    QuantityOrdered = (decimal)item.Quantity,
                    QuantityManufactured = (decimal)item.Quantity,
                    QuantityDelivered = (decimal)item.Quantity,
                    UnitOfMeasure = "pcs"
                }).ToList();

            // Auto-create delivery note draft in "Pending" status
            var deliveryNoteRequest = new CreateDeliveryNoteRequest
            {
                OrderId = orderEvent.Payload.OrderNumber,
                CustomerId = orderDetails?.CustomerId == Guid.Empty
                    ? orderEvent.Payload.CustomerId
                    : orderDetails?.CustomerId ?? orderEvent.Payload.CustomerId,
                CustomerName = orderDetails?.CustomerName,
                DeliveryDate = DateTime.UtcNow.AddDays(1), // Schedule for next day by default
                ShippingAddressId = orderDetails?.ShippingAddressId,
                ShippingAddressLine1 = orderDetails?.ShippingAddressLine1,
                ShippingAddressLine2 = orderDetails?.ShippingAddressLine2,
                ShippingCity = orderDetails?.ShippingCity,
                ShippingProvince = orderDetails?.ShippingProvince,
                ShippingPostalCode = orderDetails?.ShippingPostalCode,
                ShippingCountry = orderDetails?.ShippingCountry,
                DeliveryContactName = orderDetails?.DeliveryContactName,
                DeliveryContactPhone = orderDetails?.DeliveryContactPhone,
                DeliveryContactEmail = orderDetails?.DeliveryContactEmail,
                Items = deliveryItems
            };

            var result = await _deliveryNoteService.CreateAsync(
                deliveryNoteRequest,
                "system-auto",
                context.CancellationToken);

            await context.Publish(new DeliveryNotePdfRequestedEvent(
                Guid.NewGuid(),
                nameof(DeliveryNotePdfRequestedEvent),
                MessageType.Event,
                "1.0",
                "DeliveryService",
                ["PdfService"],
                orderEvent.CorrelationId,
                orderEvent.MessageId,
                DateTimeOffset.UtcNow,
                false,
                new DeliveryNotePdfRequestedEventPayload(
                    result.DeliveryNoteId,
                    "system-auto",
                    DateTimeOffset.UtcNow)), context.CancellationToken);

            _logger.LogInformation(
                "Auto-created delivery note and requested PDF generation: DeliveryNoteId={DeliveryNoteId}, OrderNumber={OrderNumber}",
                result.DeliveryNoteId, orderEvent.Payload.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to auto-create delivery note for OrderId={OrderId}. Event will be retried.",
                orderEvent.Payload.OrderId);
            throw; // Re-throw to trigger MassTransit retry policy
        }
    }

    private static bool IsRoutedToDeliveryService(OrderCompletedEvent message)
    {
        return message.ConsumedBy.Any(consumer =>
            string.Equals(consumer, "DeliveryService", StringComparison.OrdinalIgnoreCase));
    }
}
