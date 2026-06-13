using Maliev.DeliveryService.Domain.Entities;
using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.MessagingContracts.Contracts.Pdf;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maliev.DeliveryService.Infrastructure.Consumers;

/// <summary>
/// Consumes PDF completion events and attaches generated delivery note PDFs to delivery notes.
/// </summary>
public class PdfGenerationCompletedEventConsumer : IConsumer<PdfGenerationCompletedEvent>
{
    private readonly DeliveryDbContext _context;
    private readonly ILogger<PdfGenerationCompletedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfGenerationCompletedEventConsumer"/> class.
    /// </summary>
    /// <param name="context">Delivery database context.</param>
    /// <param name="logger">Logger instance.</param>
    public PdfGenerationCompletedEventConsumer(
        DeliveryDbContext context,
        ILogger<PdfGenerationCompletedEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<PdfGenerationCompletedEvent> context)
    {
        var payload = context.Message.Payload;
        if (payload is null)
        {
            _logger.LogWarning("Ignoring PdfGenerationCompletedEvent without payload");
            return;
        }

        if (!string.Equals(payload.DocumentType, "DeliveryNote", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "Ignoring PDF completion for document type {DocumentType}, reference {ReferenceId}",
                payload.DocumentType,
                payload.ReferenceId);
            return;
        }

        var deliveryNote = await _context.DeliveryNotes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                note => note.DeliveryNoteId == payload.ReferenceId,
                context.CancellationToken);

        if (deliveryNote == null)
        {
            _logger.LogWarning(
                "Delivery note {DeliveryNoteId} was not found for generated PDF request {RequestId}",
                payload.ReferenceId,
                payload.RequestId);
            return;
        }

        var alreadyAttached = await _context.DeliveryNoteFiles
            .AsNoTracking()
            .AnyAsync(
                file =>
                    file.DeliveryNoteId == payload.ReferenceId &&
                    file.StorageUrl == payload.StorageUrl &&
                    file.FileType == FileType.DeliveryNotePdf,
                context.CancellationToken);

        if (alreadyAttached)
        {
            _logger.LogInformation(
                "Generated delivery note PDF {StorageUrl} is already attached to delivery note {DeliveryNoteId}",
                payload.StorageUrl,
                payload.ReferenceId);
            return;
        }

        _context.DeliveryNoteFiles.Add(new DeliveryNoteFile
        {
            Id = Guid.NewGuid(),
            DeliveryNoteId = payload.ReferenceId,
            FileName = $"delivery-note-{payload.RequestId}.pdf",
            StorageUrl = payload.StorageUrl,
            ContentType = "application/pdf",
            FileSize = 0,
            FileType = FileType.DeliveryNotePdf,
            Description = "Generated delivery note PDF",
            UploadedAt = payload.CompletedAt.UtcDateTime,
            UploadedBy = "PdfService",
            IsDeleted = false
        });

        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "Attached generated PDF {StorageUrl} to delivery note {DeliveryNoteId}",
            payload.StorageUrl,
            payload.ReferenceId);
    }
}
