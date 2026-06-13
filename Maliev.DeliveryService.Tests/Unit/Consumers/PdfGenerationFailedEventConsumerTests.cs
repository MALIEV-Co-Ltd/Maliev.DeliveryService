using Maliev.DeliveryService.Domain.Entities;
using Maliev.DeliveryService.Infrastructure.Consumers;
using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.DeliveryService.Tests.Testing;
using Maliev.MessagingContracts.Contracts.Pdf;
using Maliev.MessagingContracts.Contracts.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Consumers;

[Collection("PostgreSqlDatabase")]
public class PdfGenerationFailedEventConsumerTests : IAsyncLifetime
{
    private readonly DeliveryDbContext _dbContext;
    private readonly PostgreSqlTestFixture _fixture;
    private readonly Mock<ILogger<PdfGenerationFailedEventConsumer>> _logger;

    public PdfGenerationFailedEventConsumerTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
        _dbContext = _fixture.CreateDbContext();
        _logger = new Mock<ILogger<PdfGenerationFailedEventConsumer>>();
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM delivery_note_files");
        await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM delivery_note_items");
        await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM delivery_notes");
    }

    [Fact]
    public async Task Consume_DeliveryNotePdfFailure_AppendsInternalNote()
    {
        await SeedDeliveryNoteAsync("DN-PDF-FAILED", "Existing note");
        var consumer = new PdfGenerationFailedEventConsumer(_dbContext, _logger.Object);

        await consumer.Consume(CreateConsumeContext(
            referenceId: "DN-PDF-FAILED",
            documentType: "DeliveryNote",
            requestId: "REQ-FAILED-001",
            errorMessage: "render failed").Object);

        var deliveryNote = await _dbContext.DeliveryNotes.SingleAsync(note => note.DeliveryNoteId == "DN-PDF-FAILED");
        Assert.Contains("Existing note", deliveryNote.InternalNotes);
        Assert.Contains("REQ-FAILED-001", deliveryNote.InternalNotes);
        Assert.Contains("render failed", deliveryNote.InternalNotes);
        Assert.Equal("PdfService", deliveryNote.UpdatedBy);
        Assert.NotNull(deliveryNote.UpdatedAt);
    }

    [Fact]
    public async Task Consume_DuplicateDeliveryNotePdfFailure_DoesNotAppendDuplicateNote()
    {
        await SeedDeliveryNoteAsync("DN-PDF-DUP-FAIL");
        var consumer = new PdfGenerationFailedEventConsumer(_dbContext, _logger.Object);
        var context = CreateConsumeContext(
            referenceId: "DN-PDF-DUP-FAIL",
            documentType: "DeliveryNote",
            requestId: "REQ-DUP-001",
            errorMessage: "render failed");

        await consumer.Consume(context.Object);
        await consumer.Consume(context.Object);

        var deliveryNote = await _dbContext.DeliveryNotes.SingleAsync(note => note.DeliveryNoteId == "DN-PDF-DUP-FAIL");
        Assert.Equal(1, CountOccurrences(deliveryNote.InternalNotes!, "REQ-DUP-001"));
    }

    [Fact]
    public async Task Consume_DeliveryNotePdfFailureNotRoutedToDeliveryService_DoesNotAppendInternalNote()
    {
        await SeedDeliveryNoteAsync("DN-PDF-UNROUTED", "Existing note");
        var consumer = new PdfGenerationFailedEventConsumer(_dbContext, _logger.Object);

        await consumer.Consume(CreateConsumeContext(
            referenceId: "DN-PDF-UNROUTED",
            documentType: "DeliveryNote",
            requestId: "REQ-UNROUTED-001",
            errorMessage: "render failed",
            consumedBy: ["NotificationService"]).Object);

        var deliveryNote = await _dbContext.DeliveryNotes.SingleAsync(note => note.DeliveryNoteId == "DN-PDF-UNROUTED");
        Assert.Equal("Existing note", deliveryNote.InternalNotes);
        Assert.Null(deliveryNote.UpdatedBy);
    }

    [Fact]
    public async Task Consume_NonDeliveryNotePdfFailure_DoesNotAppendInternalNote()
    {
        await SeedDeliveryNoteAsync("DN-PDF-NON-DELIVERY", "Existing note");
        var consumer = new PdfGenerationFailedEventConsumer(_dbContext, _logger.Object);

        await consumer.Consume(CreateConsumeContext(
            referenceId: "DN-PDF-NON-DELIVERY",
            documentType: "Invoice",
            requestId: "REQ-INVOICE-001",
            errorMessage: "invoice render failed").Object);

        var deliveryNote = await _dbContext.DeliveryNotes.SingleAsync(note => note.DeliveryNoteId == "DN-PDF-NON-DELIVERY");
        Assert.Equal("Existing note", deliveryNote.InternalNotes);
        Assert.Null(deliveryNote.UpdatedBy);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    private async Task SeedDeliveryNoteAsync(string deliveryNoteId, string? internalNotes = null)
    {
        _dbContext.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = deliveryNoteId,
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow,
            Status = DeliveryStatus.Pending,
            InternalNotes = internalNotes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await _dbContext.SaveChangesAsync();
    }

    private static Mock<ConsumeContext<PdfGenerationFailedEvent>> CreateConsumeContext(
        string referenceId,
        string documentType,
        string requestId,
        string errorMessage,
        string[]? consumedBy = null)
    {
        var message = new PdfGenerationFailedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(PdfGenerationFailedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "PdfService",
            ConsumedBy: consumedBy ?? ["DeliveryService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new PdfGenerationFailedEventPayload(
                RequestId: requestId,
                ReferenceId: referenceId,
                DocumentType: documentType,
                ErrorMessage: errorMessage,
                FailedAt: DateTimeOffset.UtcNow));
        var context = new Mock<ConsumeContext<PdfGenerationFailedEvent>>();
        context.Setup(c => c.Message).Returns(message);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    private static int CountOccurrences(string value, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
