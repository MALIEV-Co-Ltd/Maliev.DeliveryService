using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Infrastructure.Persistence;
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
    private readonly DeliveryDbContext _context;
    private readonly ILogger<OrderCompletedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of OrderCompletedEventConsumer.
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

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var orderEvent = context.Message;

        _logger.LogInformation(
            "Received OrderCompletedEvent: OrderId={OrderId}, OrderNumber={OrderNumber}, CompletedAt={CompletedAt}",
            orderEvent.Payload.OrderId, orderEvent.Payload.OrderNumber, orderEvent.Payload.CompletedAt);

        if (!orderEvent.Payload.JobSucceeded)
        {
            _logger.LogWarning(
                "Skipping delivery note auto-creation for failed order completion. OrderId={OrderId}, OrderNumber={OrderNumber}",
                orderEvent.Payload.OrderId,
                orderEvent.Payload.OrderNumber);
            return;
        }

        // Idempotency check: prevent duplicate delivery notes for the same order
        var existingDeliveryNote = await _context.DeliveryNotes
            .Where(dn => dn.OrderId == orderEvent.Payload.OrderNumber && !dn.IsDeleted)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (existingDeliveryNote != null)
        {
            _logger.LogWarning(
                "Delivery note already exists for OrderNumber={OrderNumber}, skipping auto-creation. Existing DeliveryNoteId={DeliveryNoteId}",
                orderEvent.Payload.OrderNumber, existingDeliveryNote.DeliveryNoteId);
            return;
        }

        try
        {
            // Auto-create delivery note draft in "Pending" status
            var deliveryNoteRequest = new CreateDeliveryNoteRequest
            {
                OrderId = orderEvent.Payload.OrderNumber,
                CustomerId = orderEvent.Payload.CustomerId,
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
                "Auto-created delivery note: DeliveryNoteId={DeliveryNoteId}, OrderNumber={OrderNumber}",
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
}
