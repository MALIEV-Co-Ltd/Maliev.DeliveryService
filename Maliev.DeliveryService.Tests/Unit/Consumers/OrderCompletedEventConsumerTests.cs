using Maliev.DeliveryService.Api.Consumers;
using Maliev.DeliveryService.Api.DTOs;
using Maliev.DeliveryService.Api.Events;
using Maliev.DeliveryService.Api.Services;
using Maliev.DeliveryService.Data;
using Maliev.DeliveryService.Data.Entities;
using Maliev.DeliveryService.Tests.Unit.TestFixtures;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Consumers;

[Collection("PostgresTests")]
public class OrderCompletedEventConsumerTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private readonly Mock<IDeliveryNoteService> _mockDeliveryService;
    private readonly Mock<ILogger<OrderCompletedEventConsumer>> _mockLogger;
    private DeliveryDbContext _context = null!;
    private readonly string _testDbName = $"consumer_test_{Guid.NewGuid():N}";

    public OrderCompletedEventConsumerTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
        _mockDeliveryService = new Mock<IDeliveryNoteService>();
        _mockLogger = new Mock<ILogger<OrderCompletedEventConsumer>>();
    }

    public async Task InitializeAsync()
    {
        _context = await _fixture.CreateIsolatedDbContextAsync(_testDbName);
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task Consume_NewOrder_ShouldCreateDeliveryNote()
    {
        var harness = new InMemoryTestHarness();

        _mockDeliveryService.Setup(x => x.CreateAsync(It.IsAny<CreateDeliveryNoteRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryNoteResponse
            {
                DeliveryNoteId = "DN-2025-001",
                Status = DeliveryStatus.Pending.ToString()
            });

        var consumer = new OrderCompletedEventConsumer(
            _mockDeliveryService.Object,
            _context,
            _mockLogger.Object);

        harness.Consumer(() => consumer);

        await harness.Start();

        try
        {
            var orderEvent = new OrderCompletedEvent
            {
                OrderId = "ORD-001",
                CustomerId = Guid.NewGuid(),
                CustomerName = "Test Customer",
                Items = new List<OrderLineItem>
                {
                    new()
                    {
                        ProductCode = "P1",
                        ProductName = "Product 1",
                        QuantityOrdered = 10,
                        QuantityManufactured = 5,
                        UnitOfMeasure = "pcs"
                    }
                }
            };

            await harness.Bus.Publish(orderEvent);

            Assert.True(await harness.Consumed.SelectAsync<OrderCompletedEvent>().Any());

            _mockDeliveryService.Verify(x => x.CreateAsync(
                It.Is<CreateDeliveryNoteRequest>(req =>
                    req.OrderId == "ORD-001" &&
                    req.Items.Count == 1 &&
                    req.Items[0].QuantityDelivered == 5),
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
        var orderId = "ORD-DUPLICATE";

        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-EXISTING",
            OrderId = orderId,
            CustomerId = Guid.NewGuid(),
            Status = DeliveryStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await _context.SaveChangesAsync();

        var harness = new InMemoryTestHarness();
        var consumer = new OrderCompletedEventConsumer(
            _mockDeliveryService.Object,
            _context,
            _mockLogger.Object);

        harness.Consumer(() => consumer);

        await harness.Start();

        try
        {
            var orderEvent = new OrderCompletedEvent
            {
                OrderId = orderId,
                CustomerId = Guid.NewGuid(),
                Items = new List<OrderLineItem>()
            };

            await harness.Bus.Publish(orderEvent);

            Assert.True(await harness.Consumed.SelectAsync<OrderCompletedEvent>().Any());

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
