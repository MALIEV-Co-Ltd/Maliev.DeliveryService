using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.MessagingContracts.Contracts.Pdf;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maliev.DeliveryService.Infrastructure.Consumers;

/// <summary>
/// Consumes PDF generation failure events and records delivery-note PDF failures durably.
/// </summary>
public class PdfGenerationFailedEventConsumer : IConsumer<PdfGenerationFailedEvent>
{
    private readonly DeliveryDbContext _context;
    private readonly ILogger<PdfGenerationFailedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfGenerationFailedEventConsumer"/> class.
    /// </summary>
    /// <param name="context">Delivery database context.</param>
    /// <param name="logger">Logger instance.</param>
    public PdfGenerationFailedEventConsumer(
        DeliveryDbContext context,
        ILogger<PdfGenerationFailedEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<PdfGenerationFailedEvent> context)
    {
        if (context.Message.ConsumedBy is not { Count: > 0 } consumedBy ||
            !consumedBy.Contains("DeliveryService", StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "Ignoring PDF failure for services {ConsumedBy}",
                context.Message.ConsumedBy is { Count: > 0 }
                    ? string.Join(",", context.Message.ConsumedBy)
                    : "(none)");
            return;
        }

        var payload = context.Message.Payload;
        if (payload is null)
        {
            _logger.LogWarning("Ignoring PdfGenerationFailedEvent without payload");
            return;
        }

        if (!string.Equals(payload.DocumentType, "DeliveryNote", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "Ignoring PDF failure for document type {DocumentType}, reference {ReferenceId}",
                payload.DocumentType,
                payload.ReferenceId);
            return;
        }

        var deliveryNote = await _context.DeliveryNotes
            .FirstOrDefaultAsync(
                note => note.DeliveryNoteId == payload.ReferenceId,
                context.CancellationToken);

        if (deliveryNote is null)
        {
            _logger.LogWarning(
                "Delivery note {DeliveryNoteId} was not found for failed PDF request {RequestId}",
                payload.ReferenceId,
                payload.RequestId);
            return;
        }

        if (deliveryNote.InternalNotes?.Contains(payload.RequestId, StringComparison.Ordinal) == true)
        {
            _logger.LogInformation(
                "Failed PDF request {RequestId} is already recorded for delivery note {DeliveryNoteId}",
                payload.RequestId,
                payload.ReferenceId);
            return;
        }

        var failureNote =
            $"PDF generation failed (request {payload.RequestId}, {payload.FailedAt:O}): {payload.ErrorMessage}";
        deliveryNote.InternalNotes = string.IsNullOrWhiteSpace(deliveryNote.InternalNotes)
            ? failureNote
            : $"{deliveryNote.InternalNotes}{Environment.NewLine}{failureNote}";
        deliveryNote.UpdatedAt = DateTime.UtcNow;
        deliveryNote.UpdatedBy = "PdfService";

        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogWarning(
            "Recorded failed PDF request {RequestId} on delivery note {DeliveryNoteId}",
            payload.RequestId,
            payload.ReferenceId);
    }
}
