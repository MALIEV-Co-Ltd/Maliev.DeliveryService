using Maliev.MessagingContracts;
using Maliev.DeliveryService.Infrastructure.Consumers;
using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Infrastructure.Services;
using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.DeliveryService.Domain.Entities;
using Maliev.DeliveryService.Tests.Testing;
using Maliev.MessagingContracts.Contracts.Delivery;
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
    private readonly Mock<IOrderServiceClient> _mockOrderServiceClient;
    private readonly DeliveryDbContext _dbContext;
    private readonly Mock<ILogger<OrderCompletedEventConsumer>> _mockLogger;

    public OrderCompletedEventConsumerTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
        _mockDeliveryService = new Mock<IDeliveryNoteService>();
        _mockOrderServiceClient = new Mock<IOrderServiceClient>();
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
            _mockOrderServiceClient.Object,
            _dbContext,
            _mockLogger.Object);

        harness.Consumer(() => consumer);

        await harness.Start();

        try
        {
            var orderId = Guid.NewGuid();
            const string orderNumber = "ORD-001";
            var customerId = Guid.NewGuid();
            var billingAddressId = Guid.NewGuid();
            var shippingAddressId = Guid.NewGuid();
            _mockOrderServiceClient
                .Setup(x => x.GetOrderAsync(orderNumber, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OrderDetailsDto
                {
                    OrderId = orderNumber,
                    OrderNumber = orderNumber,
                    CustomerId = customerId,
                    CustomerName = "Acme Manufacturing",
                    BillingAddressId = billingAddressId,
                    ShippingAddressId = shippingAddressId,
                    ShippingAddressLine1 = "88 Rama IX Road",
                    ShippingAddressLine2 = "Floor 12",
                    ShippingCity = "Bangkok",
                    ShippingProvince = "Bangkok",
                    ShippingPostalCode = "10310",
                    ShippingCountry = "TH",
                    DeliveryContactName = "Natt Customer",
                    DeliveryContactPhone = "+66810000002",
                    DeliveryContactEmail = "shipping@example.test",
                    Items =
                    [
                        new OrderLineItemDto
                        {
                            ProductCode = "P1-ENRICHED",
                            ProductName = "Enriched Product",
                            QuantityOrdered = 10,
                            QuantityManufactured = 8,
                            UnitOfMeasure = "pcs"
                        }
                    ]
                });

            var payload = new OrderCompletedEventPayload(
                orderId,
                orderNumber,
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
            Assert.True(await harness.Published.SelectAsync<DeliveryNotePdfRequestedEvent>().Any());

            var pdfRequest = await harness.Published.SelectAsync<DeliveryNotePdfRequestedEvent>().FirstOrDefault();
            Assert.NotNull(pdfRequest);
            Assert.Equal("DN-2025-001", pdfRequest.Context.Message.Payload.DeliveryNoteId);
            Assert.Equal("system-auto", pdfRequest.Context.Message.Payload.RequestedBy);
            Assert.Equal(orderEvent.CorrelationId, pdfRequest.Context.Message.CorrelationId);
            Assert.Equal(orderEvent.MessageId, pdfRequest.Context.Message.CausationId);

            _mockDeliveryService.Verify(x => x.CreateAsync(
                It.Is<CreateDeliveryNoteRequest>(req =>
                    req.OrderId == orderNumber &&
                    req.CustomerId == customerId &&
                    req.CustomerName == "Acme Manufacturing" &&
                    req.ShippingAddressId == shippingAddressId &&
                    req.ShippingAddressLine1 == "88 Rama IX Road" &&
                    req.ShippingAddressLine2 == "Floor 12" &&
                    req.ShippingCity == "Bangkok" &&
                    req.ShippingProvince == "Bangkok" &&
                    req.ShippingPostalCode == "10310" &&
                    req.ShippingCountry == "TH" &&
                    req.DeliveryContactName == "Natt Customer" &&
                    req.DeliveryContactPhone == "+66810000002" &&
                    req.DeliveryContactEmail == "shipping@example.test" &&
                    req.Items.Count == 1 &&
                    req.Items[0].ProductCode == "P1-ENRICHED" &&
                    req.Items[0].QuantityManufactured == 8m &&
                    req.Items[0].QuantityDelivered == 8m),
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
        const string orderNumber = "ORD-DUPLICATE";

        // Seed existing delivery note
        _dbContext.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-EXISTING",
            OrderId = orderNumber,
            CustomerId = Guid.NewGuid(),
            Status = DeliveryStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await _dbContext.SaveChangesAsync();

        var harness = new InMemoryTestHarness();
        var consumer = new OrderCompletedEventConsumer(
            _mockDeliveryService.Object,
            _mockOrderServiceClient.Object,
            _dbContext,
            _mockLogger.Object);

        harness.Consumer(() => consumer);

        await harness.Start();

        try
        {
            var customerId = Guid.NewGuid();
            var payload = new OrderCompletedEventPayload(
                orderId,
                orderNumber,
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
            Assert.False(await harness.Published.SelectAsync<DeliveryNotePdfRequestedEvent>().Any());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_FailedOrderCompletion_ShouldSkipDeliveryNoteCreation()
    {
        // Arrange
        var harness = new InMemoryTestHarness();
        var consumer = new OrderCompletedEventConsumer(
            _mockDeliveryService.Object,
            _mockOrderServiceClient.Object,
            _dbContext,
            _mockLogger.Object);

        harness.Consumer(() => consumer);

        await harness.Start();

        try
        {
            var payload = new OrderCompletedEventPayload(
                Guid.NewGuid(),
                "ORD-FAILED",
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                Guid.NewGuid(),
                false,
                null,
                null,
                null,
                null,
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
                It.IsAny<CreateDeliveryNoteRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
            Assert.False(await harness.Published.SelectAsync<DeliveryNotePdfRequestedEvent>().Any());
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
