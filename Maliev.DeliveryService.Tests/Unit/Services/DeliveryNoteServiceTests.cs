using Maliev.DeliveryService.Api.Clients;
using Maliev.DeliveryService.Api.DTOs;
using Maliev.DeliveryService.Api.Services;
using Maliev.DeliveryService.Data;
using Maliev.DeliveryService.Data.Entities;
using Maliev.DeliveryService.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maliev.DeliveryService.Tests.Unit.Services;

public class DeliveryNoteServiceTests : IDisposable
{
    private readonly DeliveryDbContext _context;
    private readonly DeliveryNoteService _service;
    private readonly FakePublishEndpoint _fakePublishEndpoint;
    private readonly FakeOrderServiceClient _fakeOrderServiceClient;
    private readonly FakeFileStorageService _fakeFileStorageService;
    private readonly IDistributedCache _cache;

    public DeliveryNoteServiceTests()
    {
        // Use in-memory database for unit tests
        var options = new DbContextOptionsBuilder<DeliveryDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new DeliveryDbContext(options);
        _fakePublishEndpoint = new FakePublishEndpoint();
        _fakeOrderServiceClient = new FakeOrderServiceClient();
        _fakeFileStorageService = new FakeFileStorageService();

        // Use in-memory cache
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

    [Fact]
    public async Task CreateDeliveryNoteAsync_ValidRequest_ReturnsDeliveryNote()
    {
        // Arrange
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

        // Act
        var result = await _service.CreateAsync(request, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("DN-", result.DeliveryNoteId);
        Assert.Matches(@"DN-\d{4}-\d{6}", result.DeliveryNoteId); // Format: DN-YYYY-XXXXXX
        Assert.Equal(request.OrderId, result.OrderId);
        Assert.Equal(request.CustomerId, result.CustomerId);
        Assert.Single(result.Items);

        // Verify event was published
        Assert.True(_fakePublishEndpoint.WasPublished<Maliev.MessagingContracts.Contracts.Delivery.DeliveryNoteCreatedEvent>());
    }

    [Fact]
    public async Task CreateDeliveryNoteAsync_DeliveredExceedsManufactured_ThrowsValidationException()
    {
        // Arrange
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
                    QuantityDelivered = 100, // Exceeds manufactured!
                    UnitOfMeasure = "pcs"
                }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(request, "test-user"));

        Assert.Contains("Cannot deliver more than manufactured", exception.Message);
    }

    [Fact]
    public async Task CreateDeliveryNoteAsync_NoItems_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-2026-003",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>() // Empty list!
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(request, "test-user"));

        Assert.Contains("At least one item is required", exception.Message);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_ValidTransition_UpdatesStatus()
    {
        // Arrange - Create a delivery note
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
        _fakePublishEndpoint.Clear(); // Clear creation events

        // Act - Transition Pending → InTransit
        var updateRequest = new UpdateDeliveryStatusRequest
        {
            NewStatus = "InTransit"
        };

        var result = await _service.UpdateStatusAsync(created.DeliveryNoteId, updateRequest, "test-user");

        // Assert
        Assert.Equal("InTransit", result.Status);

        // Verify status changed event was published
        var statusChangedEvents = _fakePublishEndpoint.GetPublishedMessages<Maliev.MessagingContracts.Contracts.Delivery.DeliveryStatusChangedEvent>();
        Assert.Single(statusChangedEvents);
        Assert.Equal("Pending", statusChangedEvents[0].Payload.PreviousStatus);
        Assert.Equal("InTransit", statusChangedEvents[0].Payload.NewStatus);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_InvalidTransition_ThrowsInvalidOperationException()
    {
        // Arrange - Create and transition to Delivered
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

        // Transition to InTransit first
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");

        // Transition to Delivered
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "Delivered", ReceivedByName = "John Doe" }, "test-user");

        // Act & Assert - Try to transition from Delivered → Pending (invalid!)
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateStatusAsync(created.DeliveryNoteId,
                new UpdateDeliveryStatusRequest { NewStatus = "Pending" }, "test-user"));

        Assert.Contains("Cannot transition from terminal status", exception.Message);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_ToDeliveredWithoutReceivedBy_ThrowsValidationException()
    {
        // Arrange
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

        // Act & Assert - Try to deliver without ReceivedByName
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateStatusAsync(created.DeliveryNoteId,
                new UpdateDeliveryStatusRequest { NewStatus = "Delivered" }, "test-user"));

        Assert.Contains("ReceivedByName is required", exception.Message);
    }

    [Fact]
    public async Task SoftDeleteAsync_PendingStatus_SoftDeletes()
    {
        // Arrange
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

        // Act
        await _service.SoftDeleteAsync(created.DeliveryNoteId, "test-user");

        // Assert - Verify soft delete
        var deletedNote = await _context.DeliveryNotes
            .IgnoreQueryFilters() // Bypass soft delete filter
            .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == created.DeliveryNoteId);

        Assert.NotNull(deletedNote);
        Assert.True(deletedNote.IsDeleted);
        Assert.NotNull(deletedNote.DeletedAt);
        Assert.Equal("test-user", deletedNote.DeletedBy);

        // Verify it's not returned by normal queries
        var result = await _service.GetByIdAsync(created.DeliveryNoteId);
        Assert.Null(result);
    }

    [Fact]
    public async Task SoftDeleteAsync_DeliveredStatus_ThrowsInvalidOperationException()
    {
        // Arrange
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

        // Transition to Delivered
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "Delivered", ReceivedByName = "John Doe" }, "test-user");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SoftDeleteAsync(created.DeliveryNoteId, "test-user"));

        Assert.Contains("Only Pending delivery notes can be deleted", exception.Message);
    }

    private async Task<string> SeedDeliveryNoteAsync(DeliveryStatus status = DeliveryStatus.Pending)
    {
        var id = $"DN-TEST-{Guid.NewGuid():N}";
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = id,
            OrderId = "ORD-TEST-001",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Status = status,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed-user"
        });
        await _context.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task ScanBarcodeAsync_PendingNote_SetsTrackingAndTransitionsToInTransit()
    {
        // Arrange
        var deliveryNoteId = await SeedDeliveryNoteAsync(DeliveryStatus.Pending);
        _fakePublishEndpoint.Clear();

        // Act
        var result = await _service.ScanBarcodeAsync(deliveryNoteId, "TH123456789XY", "user-42");

        // Assert
        Assert.Equal(deliveryNoteId, result.DeliveryNoteId);
        Assert.Equal("TH123456789XY", result.TrackingNumber);
        Assert.Equal("Flash Express", result.CarrierName);
        Assert.Equal("InTransit", result.Status);

        var entity = await _context.DeliveryNotes.FindAsync(deliveryNoteId);
        Assert.NotNull(entity);
        Assert.Equal(DeliveryStatus.InTransit, entity!.Status);
        Assert.Equal("TH123456789XY", entity.TrackingNumber);
        Assert.Equal("Flash Express", entity.CarrierName);
        Assert.Equal("user-42", entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedAt);

        var events = _fakePublishEndpoint
            .GetPublishedMessages<Maliev.MessagingContracts.Contracts.Delivery.DeliveryStatusChangedEvent>();
        Assert.Single(events);
        Assert.Equal("Pending", events[0].Payload.PreviousStatus);
        Assert.Equal("InTransit", events[0].Payload.NewStatus);
        Assert.Equal("user-42", events[0].Payload.ChangedBy);
    }

    [Fact]
    public async Task ScanBarcodeAsync_InTransitNote_ThrowsInvalidOperationException()
    {
        // Arrange
        var deliveryNoteId = await SeedDeliveryNoteAsync(DeliveryStatus.InTransit);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ScanBarcodeAsync(deliveryNoteId, "TH123456789XY", "user-42"));

        Assert.Equal("This delivery note has already been dispatched.", ex.Message);
    }

    [Fact]
    public async Task ScanBarcodeAsync_EmptyBarcode_ThrowsArgumentException()
    {
        // Arrange
        var deliveryNoteId = await SeedDeliveryNoteAsync(DeliveryStatus.Pending);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ScanBarcodeAsync(deliveryNoteId, "   ", "user-42"));

        Assert.Contains("must not be empty", ex.Message);
    }

    [Fact]
    public async Task ScanBarcodeAsync_NonExistentNote_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.ScanBarcodeAsync("DN-DOES-NOT-EXIST", "TH123456789XY", "user-42"));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // Fake authorization service for testing
    private class FakeAuthorizationService : IDeliveryNoteAuthorizationService
    {
        public Task<bool> CanAccessCustomerAsync(string principalId, Guid customerId, CancellationToken ct = default)
        {
            return Task.FromResult(true); // Allow all access in tests
        }

        public Task<List<Guid>> GetAuthorizedCustomerIdsAsync(string principalId, CancellationToken ct = default)
        {
            return Task.FromResult(new List<Guid>()); // Return empty list (no customer restrictions)
        }
    }
}
