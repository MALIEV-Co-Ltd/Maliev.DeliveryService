using Maliev.DeliveryService.Api.DTOs;
using Maliev.MessagingContracts.Contracts.Orders;
using Maliev.DeliveryService.Api.Services;
using Maliev.DeliveryService.Data;
using Maliev.MessagingContracts.Contracts.Delivery;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.DeliveryService.Api.Consumers;

/// <summary>
/// Consumes OrderCompletedEvent to auto-create delivery note drafts
/// </summary>
public class OrderCompletedEventConsumer : IConsumer<OrderCompletedEvent>
{
    private readonly IDeliveryNoteService _deliveryNoteService;
    private readonly DeliveryDbContext _context;
    private readonly ILogger<OrderCompletedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the OrderCompletedEventConsumer class.
    /// </summary>
    public OrderCompletedEventConsumer(
        IDeliveryNoteService deliveryNoteService,
        DeliveryDbContext context,
        ILogger<OrderCompletedEventConsumer> logger)
    {
        _deliveryNoteService = deliveryNoteService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Consumes the OrderCompletedEvent.
    /// </summary>
    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var orderEvent = context.Message.Payload;

        _logger.LogInformation(
            "Received OrderCompletedEvent: OrderId={OrderId}, CustomerId={CustomerId}, ItemCount={ItemCount}",
            orderEvent.OrderId, orderEvent.CustomerId, orderEvent.Items.Count);

        // Idempotency check: prevent duplicate delivery notes for the same order
        var existingDeliveryNote = await _context.DeliveryNotes
            .Where(dn => dn.OrderId == orderEvent.OrderId.ToString() && !dn.IsDeleted)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (existingDeliveryNote != null)
        {
            _logger.LogWarning(
                "Delivery note already exists for OrderId={OrderId}, skipping auto-creation. Existing DeliveryNoteId={DeliveryNoteId}",
                orderEvent.OrderId, existingDeliveryNote.DeliveryNoteId);
            return;
        }

        try
        {
            // Auto-create delivery note draft in "Pending" status
            var deliveryNoteRequest = new CreateDeliveryNoteRequest
            {
                OrderId = orderEvent.OrderId.ToString(),
                CustomerId = orderEvent.CustomerId,
                CustomerName = orderEvent.CustomerName,
                DeliveryDate = DateTime.UtcNow.AddDays(1), // Schedule for next day by default
                Items = orderEvent.Items.Select(item => new CreateDeliveryNoteItemRequest
                {
                    OrderId = orderEvent.OrderId.ToString(),
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    QuantityOrdered = (decimal)item.QuantityOrdered,
                    QuantityManufactured = (decimal)item.QuantityManufactured,
                    QuantityDelivered = (decimal)item.QuantityManufactured, // Default: deliver all manufactured
                    UnitOfMeasure = item.UnitOfMeasure
                }).ToList()
            };

            var result = await _deliveryNoteService.CreateAsync(
                deliveryNoteRequest,
                "system-auto",
                context.CancellationToken);

            _logger.LogInformation(
                "Auto-created delivery note: DeliveryNoteId={DeliveryNoteId}, OrderId={OrderId}, CustomerId={CustomerId}",
                result.DeliveryNoteId, orderEvent.OrderId, orderEvent.CustomerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to auto-create delivery note for OrderId={OrderId}. Event will be retried.",
                orderEvent.OrderId);
            throw; // Re-throw to trigger MassTransit retry policy
        }
    }
}
