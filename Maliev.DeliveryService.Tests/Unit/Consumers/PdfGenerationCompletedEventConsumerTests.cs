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
public class PdfGenerationCompletedEventConsumerTests : IAsyncLifetime
{
    private readonly DeliveryDbContext _dbContext;
    private readonly PostgreSqlTestFixture _fixture;
    private readonly Mock<ILogger<PdfGenerationCompletedEventConsumer>> _logger;

    public PdfGenerationCompletedEventConsumerTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
        _dbContext = _fixture.CreateDbContext();
        _logger = new Mock<ILogger<PdfGenerationCompletedEventConsumer>>();
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM delivery_note_files");
        await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM delivery_note_items");
        await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM delivery_notes");
    }

    [Fact]
    public async Task Consume_DeliveryNotePdfCompletion_AttachesPdfFile()
    {
        await SeedDeliveryNoteAsync("DN-PDF-001");
        var consumer = new PdfGenerationCompletedEventConsumer(_dbContext, _logger.Object);
        var completedAt = DateTimeOffset.UtcNow;

        await consumer.Consume(CreateConsumeContext(
            referenceId: "DN-PDF-001",
            documentType: "DeliveryNote",
            storageUrl: "https://storage.example/dn.pdf",
            completedAt: completedAt).Object);

        var file = await _dbContext.DeliveryNoteFiles.SingleAsync(file => file.DeliveryNoteId == "DN-PDF-001");
        Assert.Equal(FileType.DeliveryNotePdf, file.FileType);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("https://storage.example/dn.pdf", file.StorageUrl);
        Assert.Equal("PdfService", file.UploadedBy);
        Assert.Equal(completedAt.UtcDateTime, file.UploadedAt);
    }

    [Fact]
    public async Task Consume_DuplicateDeliveryNotePdfCompletion_DoesNotAttachDuplicate()
    {
        await SeedDeliveryNoteAsync("DN-PDF-DUP");
        _dbContext.DeliveryNoteFiles.Add(new DeliveryNoteFile
        {
            Id = Guid.NewGuid(),
            DeliveryNoteId = "DN-PDF-DUP",
            FileName = "delivery-note-existing.pdf",
            StorageUrl = "https://storage.example/dn.pdf",
            ContentType = "application/pdf",
            FileSize = 0,
            FileType = FileType.DeliveryNotePdf,
            UploadedAt = DateTime.UtcNow,
            UploadedBy = "PdfService"
        });
        await _dbContext.SaveChangesAsync();
        var consumer = new PdfGenerationCompletedEventConsumer(_dbContext, _logger.Object);

        await consumer.Consume(CreateConsumeContext(
            referenceId: "DN-PDF-DUP",
            documentType: "DeliveryNote",
            storageUrl: "https://storage.example/dn.pdf",
            completedAt: DateTimeOffset.UtcNow).Object);

        var fileCount = await _dbContext.DeliveryNoteFiles.CountAsync(file => file.DeliveryNoteId == "DN-PDF-DUP");
        Assert.Equal(1, fileCount);
    }

    [Fact]
    public async Task Consume_NonDeliveryNotePdfCompletion_DoesNotAttachFile()
    {
        await SeedDeliveryNoteAsync("DN-PDF-IGNORE");
        var consumer = new PdfGenerationCompletedEventConsumer(_dbContext, _logger.Object);

        await consumer.Consume(CreateConsumeContext(
            referenceId: "DN-PDF-IGNORE",
            documentType: "Invoice",
            storageUrl: "https://storage.example/invoice.pdf",
            completedAt: DateTimeOffset.UtcNow).Object);

        Assert.Empty(await _dbContext.DeliveryNoteFiles.ToListAsync());
    }

    [Fact]
    public async Task Consume_DeliveryNotePdfCompletionNotRoutedToDeliveryService_DoesNotAttachFile()
    {
        await SeedDeliveryNoteAsync("DN-PDF-UNROUTED");
        var consumer = new PdfGenerationCompletedEventConsumer(_dbContext, _logger.Object);

        await consumer.Consume(CreateConsumeContext(
            referenceId: "DN-PDF-UNROUTED",
            documentType: "DeliveryNote",
            storageUrl: "https://storage.example/dn-unrouted.pdf",
            completedAt: DateTimeOffset.UtcNow,
            consumedBy: ["NotificationService"]).Object);

        Assert.Empty(await _dbContext.DeliveryNoteFiles.ToListAsync());
    }

    [Fact]
    public async Task Consume_MissingDeliveryNote_DoesNotAttachFile()
    {
        var consumer = new PdfGenerationCompletedEventConsumer(_dbContext, _logger.Object);

        await consumer.Consume(CreateConsumeContext(
            referenceId: "DN-MISSING",
            documentType: "DeliveryNote",
            storageUrl: "https://storage.example/dn.pdf",
            completedAt: DateTimeOffset.UtcNow).Object);

        Assert.Empty(await _dbContext.DeliveryNoteFiles.ToListAsync());
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    private async Task SeedDeliveryNoteAsync(string deliveryNoteId)
    {
        _dbContext.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = deliveryNoteId,
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow,
            Status = DeliveryStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await _dbContext.SaveChangesAsync();
    }

    private static Mock<ConsumeContext<PdfGenerationCompletedEvent>> CreateConsumeContext(
        string referenceId,
        string documentType,
        string storageUrl,
        DateTimeOffset completedAt,
        string[]? consumedBy = null)
    {
        var message = new PdfGenerationCompletedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(PdfGenerationCompletedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "PdfService",
            ConsumedBy: consumedBy ?? ["DeliveryService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new PdfGenerationCompletedEventPayload(
                RequestId: Guid.NewGuid().ToString(),
                ReferenceId: referenceId,
                DocumentType: documentType,
                StorageUrl: storageUrl,
                CompletedAt: completedAt));
        var context = new Mock<ConsumeContext<PdfGenerationCompletedEvent>>();
        context.Setup(c => c.Message).Returns(message);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }
}
