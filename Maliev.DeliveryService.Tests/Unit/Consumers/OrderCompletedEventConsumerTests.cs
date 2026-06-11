using Maliev.MessagingContracts;
using Maliev.DeliveryService.Infrastructure.Consumers;
using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Infrastructure.Services;
using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.DeliveryService.Domain.Entities;
using Maliev.DeliveryService.Tests.Testing;
using Maliev.MessagingContracts.Contracts.Orders;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Consumers;

[Collection("PostgreSqlDatabase")]
public class OrderCompletedEventConsumerTests : IAsyncLifetime
{
    private readonly PostgreSqlTestFixture _fixture;
    private readonly Mock<IDeliveryNoteService> _mockDeliveryService;
    private readonly DeliveryDbContext _dbContext;
    private readonly Mock<ILogger<OrderCompletedEventConsumer>> _mockLogger;

    public OrderCompletedEventConsumerTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
        _mockDeliveryService = new Mock<IDeliveryNoteService>();
        _mockLogger = new Mock<ILogger<OrderCompletedEventConsumer>>();

        _dbContext = _fixture.CreateDbContext();
    }

    public async Task InitializeAsync()
    {
        // Clean up tables before each test
        await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM delivery_note_files");
        await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM delivery_note_items");
        await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM delivery_notes");
    }

    [Fact]
    public async Task Consume_NewOrder_ShouldCreateDeliveryNote()
    {
        // Arrange
        var harness = new InMemoryTestHarness();

        // Setup service mock
        _mockDeliveryService.Setup(x => x.CreateAsync(It.IsAny<CreateDeliveryNoteRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryNoteResponse
            {
                DeliveryNoteId = "DN-2025-001",
                Status = DeliveryStatus.Pending.ToString()
            });

        var consumer = new OrderCompletedEventConsumer(
            _mockDeliveryService.Object,
            _dbContext,
            _mockLogger.Object);

        harness.Consumer(() => consumer);

        await harness.Start();

        try
        {
            var orderId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var payload = new OrderCompletedEventPayload(
                orderId,
                "ORD-001",
                customerId,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow.AddHours(-1),
                DateTimeOffset.UtcNow,
                Guid.NewGuid(),
                true,
                null, null, null, null,
                new List<OrderCompletedEventPayloadItemsItem>
                {
                    new(Guid.NewGuid(), "P1", "Product 1", 10.0, 100.0, 1000.0)
                });

            var orderEvent = new OrderCompletedEvent(
                Guid.NewGuid(),
                nameof(OrderCompletedEvent),
                MessageType.Event,
                "1.0",
                "OrderService",
                Array.Empty<string>(),
                Guid.NewGuid(),
                null,
                DateTimeOffset.UtcNow,
                false,
                payload);

            // Act
            await harness.Bus.Publish(orderEvent);

            // Assert
            Assert.True(await harness.Consumed.SelectAsync<OrderCompletedEvent>().Any());

            _mockDeliveryService.Verify(x => x.CreateAsync(
                It.Is<CreateDeliveryNoteRequest>(req =>
                    req.OrderId == orderId.ToString() &&
                    req.CustomerId == customerId &&
                    req.Items.Count == 1 &&
                    req.Items[0].QuantityDelivered == 10.0m),
                "system-auto",
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_DuplicateOrder_ShouldSkipCreation()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Seed existing delivery note
        _dbContext.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-EXISTING",
            OrderId = orderId.ToString(),
            CustomerId = Guid.NewGuid(),
            Status = DeliveryStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await _dbContext.SaveChangesAsync();

        var harness = new InMemoryTestHarness();
        var consumer = new OrderCompletedEventConsumer(
            _mockDeliveryService.Object,
            _dbContext,
            _mockLogger.Object);

        harness.Consumer(() => consumer);

        await harness.Start();

        try
        {
            var customerId = Guid.NewGuid();
            var payload = new OrderCompletedEventPayload(
                orderId,
                "ORD-DUPLICATE",
                customerId,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                Guid.NewGuid(),
                true,
                null, null, null, null,
                new List<OrderCompletedEventPayloadItemsItem>());

            var orderEvent = new OrderCompletedEvent(
                Guid.NewGuid(),
                nameof(OrderCompletedEvent),
                MessageType.Event,
                "1.0",
                "OrderService",
                Array.Empty<string>(),
                Guid.NewGuid(),
                null,
                DateTimeOffset.UtcNow,
                false,
                payload);

            // Act
            await harness.Bus.Publish(orderEvent);

            // Assert
            Assert.True(await harness.Consumed.SelectAsync<OrderCompletedEvent>().Any());

            // Verify CreateAsync was NEVER called
            _mockDeliveryService.Verify(x => x.CreateAsync(
                It.IsAny<CreateDeliveryNoteRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            await harness.Stop();
        }
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }
}
