using Maliev.DeliveryService.Api.Consumers;
using Maliev.DeliveryService.Api.DTOs;
using Maliev.MessagingContracts.Contracts.Orders;
using Maliev.MessagingContracts.Generated;
using Maliev.DeliveryService.Api.Services;
using Maliev.DeliveryService.Data;
using Maliev.DeliveryService.Data.Entities;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Consumers;

public class OrderCompletedEventConsumerTests
{
    private readonly Mock<IDeliveryNoteService> _mockDeliveryService;
    private readonly DeliveryDbContext _dbContext;
    private readonly Mock<ILogger<OrderCompletedEventConsumer>> _mockLogger;

    public OrderCompletedEventConsumerTests()
    {
        _mockDeliveryService = new Mock<IDeliveryNoteService>();
        _mockLogger = new Mock<ILogger<OrderCompletedEventConsumer>>();

        // Create a unique DB name for each test instance to avoid collisions
        var options = new DbContextOptionsBuilder<DeliveryDbContext>()
            .UseInMemoryDatabase(databaseName: $"ConsumerTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new DeliveryDbContext(options);
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
            var orderEvent = new OrderCompletedEvent(
                MessageId: Guid.NewGuid(),
                MessageName: nameof(OrderCompletedEvent),
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "Test",
                ConsumedBy: Array.Empty<string>(),
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new OrderCompletedEventPayload
                {
                    OrderId = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Test Customer",
                    Items = new List<OrderCompletedEventPayloadItemsItem>
                    {
                        new()
                        {
                            ProductCode = "P1",
                            ProductName = "Product 1",
                            QuantityOrdered = 10,
                            QuantityManufactured = 5,
                            UnitOfMeasure = "pcs"
                        }
                    },
                    CompletedAt = DateTimeOffset.UtcNow
                }
            );

            // Act
            await harness.Bus.Publish(orderEvent);

            // Assert
            Assert.True(await harness.Consumed.SelectAsync<OrderCompletedEvent>().Any());

            _mockDeliveryService.Verify(x => x.CreateAsync(
                It.Is<CreateDeliveryNoteRequest>(req =>
                    req.OrderId == orderEvent.Payload.OrderId.ToString() &&
                    req.Items.Count == 1 &&
                    req.Items[0].QuantityDelivered == 5), // Expects manufactured quantity
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
            var orderEvent = new OrderCompletedEvent(
                MessageId: Guid.NewGuid(),
                MessageName: nameof(OrderCompletedEvent),
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "Test",
                ConsumedBy: Array.Empty<string>(),
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new OrderCompletedEventPayload
                {
                    OrderId = orderId,
                    CustomerId = Guid.NewGuid(),
                    Items = new List<OrderCompletedEventPayloadItemsItem>(),
                    CompletedAt = DateTimeOffset.UtcNow
                }
            );

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
}
