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
using System.Text.Json;
using Moq;

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

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsFromCache()
    {
        // Arrange
        var deliveryNoteId = "DN-2026-CACHE";
        var response = new DeliveryNoteResponse
        {
            DeliveryNoteId = deliveryNoteId,
            OrderId = "ORD-CACHE",
            Status = "Pending"
        };

        var cacheKey = $"delivery-note:{deliveryNoteId}";
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response));

        // Act
        var result = await _service.GetByIdAsync(deliveryNoteId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(deliveryNoteId, result.DeliveryNoteId);
        Assert.Equal("ORD-CACHE", result.OrderId);
    }

    [Fact]
    public async Task SearchAsync_WithFilters_ReturnsPaginatedResult()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-S1",
            OrderId = "ORD-S1",
            CustomerId = customerId,
            CustomerName = "Search Test",
            Status = DeliveryStatus.Pending,
            DeliveryDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await _context.SaveChangesAsync();

        var filter = new DeliveryNoteFilterRequest
        {
            CustomerId = customerId,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchAsync(filter, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("DN-S1", result.Items[0].DeliveryNoteId);
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesFields()
    {
        // Arrange
        var createRequest = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-UPDATE",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest> { new() { ProductCode = "P1", QuantityOrdered = 10, QuantityManufactured = 10, QuantityDelivered = 5, UnitOfMeasure = "pcs" } }
        };
        var created = await _service.CreateAsync(createRequest, "test-user");

        var updateRequest = new UpdateDeliveryNoteRequest
        {
            CarrierName = "New Carrier",
            TrackingNumber = "TRACK123",
            RowVersion = (await _context.DeliveryNotes.FirstAsync(dn => dn.DeliveryNoteId == created.DeliveryNoteId)).RowVersion
        };

        // Act
        var result = await _service.UpdateAsync(created.DeliveryNoteId, updateRequest, "test-user");

        // Assert
        Assert.Equal("New Carrier", result.CarrierName);
        Assert.Equal("TRACK123", result.TrackingNumber);
    }

    [Fact]
    public async Task UpdateAsync_ConcurrencyConflict_ThrowsException()
    {
        // Arrange
        var createRequest = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-CONFLICT",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest> { new() { ProductCode = "P1", QuantityOrdered = 10, QuantityManufactured = 10, QuantityDelivered = 5, UnitOfMeasure = "pcs" } }
        };
        var created = await _service.CreateAsync(createRequest, "test-user");

        var updateRequest = new UpdateDeliveryNoteRequest
        {
            CarrierName = "New Carrier",
            RowVersion = 999 // Wrong row version
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(created.DeliveryNoteId, updateRequest, "test-user"));
    }

    [Fact]
    public async Task AddFileAsync_ValidFile_AddsToFileList()
    {
        // Arrange
        var createRequest = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-FILE",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest> { new() { ProductCode = "P1", QuantityOrdered = 10, QuantityManufactured = 10, QuantityDelivered = 5, UnitOfMeasure = "pcs" } }
        };
        var created = await _service.CreateAsync(createRequest, "test-user");

        var fileMock = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        var content = "fake content";
        var fileName = "test.png";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.ContentType).Returns("image/png");

        // Act
        var result = await _service.AddFileAsync(created.DeliveryNoteId, fileMock.Object, FileType.Photo, "Test file", "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fileName, result.OriginalFileName);

        var files = await _service.GetFilesAsync(created.DeliveryNoteId);
        Assert.Single(files);
        Assert.Equal(fileName, files[0].OriginalFileName);
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
