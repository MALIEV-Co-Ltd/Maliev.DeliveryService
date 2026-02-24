using Maliev.DeliveryService.Api.DTOs;
using Maliev.DeliveryService.Api.Services;
using Maliev.DeliveryService.Data;
using Maliev.MessagingContracts.Contracts.Orders;
using Maliev.MessagingContracts.Generated;
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

    public OrderCompletedEventConsumer(
        IDeliveryNoteService deliveryNoteService,
        DeliveryDbContext context,
        ILogger<OrderCompletedEventConsumer> logger)
    {
        _deliveryNoteService = deliveryNoteService;
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var orderEvent = context.Message;

        _logger.LogInformation(
            "Received OrderCompletedEvent: OrderId={OrderId}, OrderNumber={OrderNumber}, CompletedAt={CompletedAt}",
            orderEvent.Payload.OrderId, orderEvent.Payload.OrderNumber, orderEvent.Payload.CompletedAt);

        // Idempotency check: prevent duplicate delivery notes for the same order
        var existingDeliveryNote = await _context.DeliveryNotes
            .Where(dn => dn.OrderId == orderEvent.Payload.OrderId.ToString() && !dn.IsDeleted)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (existingDeliveryNote != null)
        {
            _logger.LogWarning(
                "Delivery note already exists for OrderId={OrderId}, skipping auto-creation. Existing DeliveryNoteId={DeliveryNoteId}",
                orderEvent.Payload.OrderId, existingDeliveryNote.DeliveryNoteId);
            return;
        }

        try
        {
            // Auto-create delivery note draft in "Pending" status
            var deliveryNoteRequest = new CreateDeliveryNoteRequest
            {
                OrderId = orderEvent.Payload.OrderId.ToString(),
                CustomerId = Guid.Empty, // Not in OrderCompletedEvent payload
                CustomerName = "Pending", // Minimal, placeholder
                DeliveryDate = DateTime.UtcNow.AddDays(1), // Schedule for next day by default
                Items = orderEvent.Payload.Items.Select(item => new CreateDeliveryNoteItemRequest
                {
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    QuantityOrdered = (decimal)item.Quantity,
                    QuantityManufactured = (decimal)item.Quantity, // Assume all manufactured for draft
                    QuantityDelivered = (decimal)item.Quantity,    // Assume full delivery for draft
                    UnitOfMeasure = "pcs" // Default UoM, contract doesn't have it for items
                }).ToList()
            };

            var result = await _deliveryNoteService.CreateAsync(
                deliveryNoteRequest,
                "system-auto",
                context.CancellationToken);

            _logger.LogInformation(
                "Auto-created delivery note: DeliveryNoteId={DeliveryNoteId}, OrderId={OrderId}",
                result.DeliveryNoteId, orderEvent.Payload.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to auto-create delivery note for OrderId={OrderId}. Event will be retried.",
                orderEvent.Payload.OrderId);
            throw; // Re-throw to trigger MassTransit retry policy
        }
    }
}
