using Maliev.DeliveryService.Api.Clients;
using Maliev.DeliveryService.Api.DTOs;
using Maliev.DeliveryService.Api.Services;
using Maliev.DeliveryService.Data;
using Maliev.DeliveryService.Data.Entities;
using Maliev.DeliveryService.Tests.Fakes;
using Maliev.DeliveryService.Tests.Unit.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Services;

[Collection("PostgresTests")]
public class DeliveryNoteServiceTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private DeliveryDbContext _context = null!;
    private DeliveryNoteService _service = null!;
    private FakePublishEndpoint _fakePublishEndpoint = null!;
    private FakeOrderServiceClient _fakeOrderServiceClient = null!;
    private FakeFileStorageService _fakeFileStorageService = null!;
    private IDistributedCache _cache = null!;
    private readonly string _testDbName = $"delivery_test_{Guid.NewGuid():N}";

    public DeliveryNoteServiceTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _context = await _fixture.CreateIsolatedDbContextAsync(_testDbName);
        _fakePublishEndpoint = new FakePublishEndpoint();
        _fakeOrderServiceClient = new FakeOrderServiceClient();
        _fakeFileStorageService = new FakeFileStorageService();

        _cache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions()));

        var idGenerator = new DeliveryNoteIdGenerator(_context);
        var authService = new FakeAuthorizationService();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<DeliveryNoteService>.Instance;

        _service = new DeliveryNoteService(
            _context,
            idGenerator,
            _fakePublishEndpoint,
            _fakeOrderServiceClient,
            _cache,
            authService,
            _fakeFileStorageService,
            logger);
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task CreateDeliveryNoteAsync_ValidRequest_ReturnsDeliveryNote()
    {
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-2026-001",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "PROD-001",
                    ProductName = "Test Product",
                    QuantityOrdered = 100,
                    QuantityManufactured = 100,
                    QuantityDelivered = 100,
                    UnitOfMeasure = "pcs"
                }
            }
        };

        var result = await _service.CreateAsync(request, "test-user");

        Assert.NotNull(result);
        Assert.StartsWith("DN-", result.DeliveryNoteId);
        Assert.Matches(@"DN-\d{4}-\d{6}", result.DeliveryNoteId);
        Assert.Equal(request.OrderId, result.OrderId);
        Assert.Equal(request.CustomerId, result.CustomerId);
        Assert.Single(result.Items);

        Assert.True(_fakePublishEndpoint.WasPublished<MessagingContracts.Contracts.Delivery.DeliveryNoteCreatedEvent>());
    }

    [Fact]
    public async Task CreateDeliveryNoteAsync_DeliveredExceedsManufactured_ThrowsValidationException()
    {
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-2026-002",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "PROD-001",
                    ProductName = "Test Product",
                    QuantityOrdered = 100,
                    QuantityManufactured = 80,
                    QuantityDelivered = 100,
                    UnitOfMeasure = "pcs"
                }
            }
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(request, "test-user"));

        Assert.Contains("Cannot deliver more than manufactured", exception.Message);
    }

    [Fact]
    public async Task CreateDeliveryNoteAsync_NoItems_ThrowsValidationException()
    {
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-2026-003",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>()
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(request, "test-user"));

        Assert.Contains("At least one item is required", exception.Message);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_ValidTransition_UpdatesStatus()
    {
        var createRequest = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-2026-004",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "PROD-001",
                    ProductName = "Test Product",
                    QuantityOrdered = 100,
                    QuantityManufactured = 100,
                    QuantityDelivered = 100,
                    UnitOfMeasure = "pcs"
                }
            }
        };

        var created = await _service.CreateAsync(createRequest, "test-user");
        _fakePublishEndpoint.Clear();

        var updateRequest = new UpdateDeliveryStatusRequest
        {
            NewStatus = "InTransit"
        };

        var result = await _service.UpdateStatusAsync(created.DeliveryNoteId, updateRequest, "test-user");

        Assert.Equal("InTransit", result.Status);

        var statusChangedEvents = _fakePublishEndpoint.GetPublishedMessages<MessagingContracts.Contracts.Delivery.DeliveryStatusChangedEvent>();
        Assert.Single(statusChangedEvents);
        Assert.Equal("Pending", statusChangedEvents[0].PreviousStatus);
        Assert.Equal("InTransit", statusChangedEvents[0].NewStatus);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_InvalidTransition_ThrowsInvalidOperationException()
    {
        var createRequest = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-2026-005",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "PROD-001",
                    ProductName = "Test Product",
                    QuantityOrdered = 100,
                    QuantityManufactured = 100,
                    QuantityDelivered = 100,
                    UnitOfMeasure = "pcs"
                }
            }
        };

        var created = await _service.CreateAsync(createRequest, "test-user");

        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");

        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "Delivered", ReceivedByName = "John Doe" }, "test-user");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateStatusAsync(created.DeliveryNoteId,
                new UpdateDeliveryStatusRequest { NewStatus = "Pending" }, "test-user"));

        Assert.Contains("Cannot transition from terminal status", exception.Message);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_ToDeliveredWithoutReceivedBy_ThrowsValidationException()
    {
        var createRequest = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-2026-006",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "PROD-001",
                    ProductName = "Test Product",
                    QuantityOrdered = 100,
                    QuantityManufactured = 100,
                    QuantityDelivered = 100,
                    UnitOfMeasure = "pcs"
                }
            }
        };

        var created = await _service.CreateAsync(createRequest, "test-user");

        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateStatusAsync(created.DeliveryNoteId,
                new UpdateDeliveryStatusRequest { NewStatus = "Delivered" }, "test-user"));

        Assert.Contains("ReceivedByName is required", exception.Message);
    }

    [Fact]
    public async Task SoftDeleteAsync_PendingStatus_SoftDeletes()
    {
        var createRequest = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-2026-007",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "PROD-001",
                    ProductName = "Test Product",
                    QuantityOrdered = 100,
                    QuantityManufactured = 100,
                    QuantityDelivered = 100,
                    UnitOfMeasure = "pcs"
                }
            }
        };

        var created = await _service.CreateAsync(createRequest, "test-user");

        await _service.SoftDeleteAsync(created.DeliveryNoteId, "test-user");

        var deletedNote = await _context.DeliveryNotes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == created.DeliveryNoteId);

        Assert.NotNull(deletedNote);
        Assert.True(deletedNote.IsDeleted);
        Assert.NotNull(deletedNote.DeletedAt);
        Assert.Equal("test-user", deletedNote.DeletedBy);

        var result = await _service.GetByIdAsync(created.DeliveryNoteId);
        Assert.Null(result);
    }

    [Fact]
    public async Task SoftDeleteAsync_DeliveredStatus_ThrowsInvalidOperationException()
    {
        var createRequest = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-2026-008",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "PROD-001",
                    ProductName = "Test Product",
                    QuantityOrdered = 100,
                    QuantityManufactured = 100,
                    QuantityDelivered = 100,
                    UnitOfMeasure = "pcs"
                }
            }
        };

        var created = await _service.CreateAsync(createRequest, "test-user");

        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "Delivered", ReceivedByName = "John Doe" }, "test-user");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SoftDeleteAsync(created.DeliveryNoteId, "test-user"));

        Assert.Contains("Only Pending delivery notes can be deleted", exception.Message);
    }

    private class FakeAuthorizationService : IDeliveryNoteAuthorizationService
    {
        public Task<bool> CanAccessCustomerAsync(string principalId, Guid customerId, CancellationToken ct = default)
        {
            return Task.FromResult(true);
        }

        public Task<List<Guid>> GetAuthorizedCustomerIdsAsync(string principalId, CancellationToken ct = default)
        {
            return Task.FromResult(new List<Guid>());
        }
    }
}
