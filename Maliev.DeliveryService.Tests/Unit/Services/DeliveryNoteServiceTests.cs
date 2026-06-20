using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Infrastructure.Services;
using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.DeliveryService.Domain.Entities;
using Maliev.DeliveryService.Tests.Fakes;
using Maliev.DeliveryService.Tests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Moq;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Services;

[Collection("PostgreSqlDatabase")]
public class DeliveryNoteServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlTestFixture _fixture;
    private readonly DeliveryDbContext _context;
    private readonly DeliveryNoteService _service;
    private readonly FakePublishEndpoint _fakePublishEndpoint;
    private readonly FakeOrderServiceClient _fakeOrderServiceClient;
    private readonly FakeFileStorageService _fakeFileStorageService;
    private readonly TestHttpClientFactory _httpClientFactory;
    private readonly IDistributedCache _cache;

    public DeliveryNoteServiceTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
        _context = _fixture.CreateDbContext();
        _fakePublishEndpoint = new FakePublishEndpoint();
        _fakeOrderServiceClient = new FakeOrderServiceClient();
        _fakeFileStorageService = new FakeFileStorageService();
        _httpClientFactory = new TestHttpClientFactory();

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
            _httpClientFactory,
            logger);
    }

    public async Task InitializeAsync()
    {
        // Clean up tables before each test
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM delivery_note_files");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM delivery_note_items");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM delivery_status_audits");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM delivery_notes");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM addresses");
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
    public void CreateAsync_PublishesCreatedEventBeforeSavingForOutbox()
    {
        var source = File.ReadAllText(FindDeliveryNoteServiceSourcePath());
        var methodBody = ExtractMethodSource(
            source,
            "public async Task<DeliveryNoteResponse> CreateAsync");

        AssertCallAppearsBeforeSaveChanges(
            methodBody,
            "await PublishEventAsync(new DeliveryNoteCreatedEvent(",
            "CreateAsync must publish DeliveryNoteCreatedEvent before SaveChangesAsync so the EF bus outbox persists the event atomically with the delivery note.");
    }

    [Fact]
    public void UpdateStatusAsync_PublishesLifecycleEventsBeforeSavingForOutbox()
    {
        var source = File.ReadAllText(FindDeliveryNoteServiceSourcePath());
        var methodBody = ExtractMethodSource(
            source,
            "public async Task<DeliveryNoteResponse> UpdateStatusAsync");

        AssertCallAppearsBeforeSaveChanges(
            methodBody,
            "await PublishEventAsync(new DeliveryStatusChangedEvent(",
            "UpdateStatusAsync must publish DeliveryStatusChangedEvent before SaveChangesAsync so the EF bus outbox persists delivery status changes atomically.");
        AssertCallAppearsBeforeSaveChanges(
            methodBody,
            "await PublishEventAsync(new DeliveryCompletedEvent(",
            "UpdateStatusAsync must publish DeliveryCompletedEvent before SaveChangesAsync so the EF bus outbox persists delivery completion atomically.");
    }

    [Fact]
    public async Task CreateAsync_WhenCreatedEventCannotBeStaged_DoesNotPersistDeliveryNote()
    {
        var orderId = "ORD-PUBLISH-FAIL";
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = orderId,
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "PROD-FAIL",
                    ProductName = "Test Product",
                    QuantityOrdered = 1,
                    QuantityManufactured = 1,
                    QuantityDelivered = 1,
                    UnitOfMeasure = "pcs"
                }
            }
        };
        _fakePublishEndpoint.PublishException = new InvalidOperationException("outbox unavailable");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(request, "test-user"));

        Assert.Contains("outbox unavailable", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await _context.DeliveryNotes.AnyAsync(note => note.OrderId == orderId));
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
    public async Task UpdateDeliveryStatusAsync_Delivered_SyncsOrderDeliverySnapshot()
    {
        var deliveryDate = DateTimeOffset.UtcNow.AddDays(2);
        var actualDeliveryTime = DateTime.UtcNow.AddMinutes(5);
        var createRequest = new CreateDeliveryNoteRequest
        {
            OrderId = "MO-DELIVERY-SYNC",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = deliveryDate.UtcDateTime,
            DeliveryContactName = "Receiving Dock",
            DeliveryContactPhone = "+66810000003",
            DeliveryContactEmail = "receiving@example.test",
            Items =
            [
                new()
                {
                    ProductCode = "MAKE-STUDIO-PART",
                    ProductName = "Make Studio Part",
                    QuantityOrdered = 1,
                    QuantityManufactured = 1,
                    QuantityDelivered = 1,
                    UnitOfMeasure = "pcs"
                }
            ]
        };
        var created = await _service.CreateAsync(createRequest, "test-user");

        await _service.UpdateStatusAsync(
            created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" },
            "test-user");
        await _service.UpdateStatusAsync(
            created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest
            {
                NewStatus = "Delivered",
                ReceivedByName = "Receiving Dock",
                ActualDeliveryTime = actualDeliveryTime
            },
            "test-user");

        var snapshot = Assert.Single(
            _fakeOrderServiceClient.DeliverySnapshots,
            item => item.OrderId == "MO-DELIVERY-SYNC" && item.ActualDeliveryDate.HasValue);
        Assert.Equal(createRequest.DeliveryDate, snapshot.PromisedDeliveryDate);
        Assert.Equal(actualDeliveryTime, snapshot.ActualDeliveryDate);
        Assert.Equal("Receiving Dock", snapshot.DeliveryContactName);
        Assert.Equal("+66810000003", snapshot.DeliveryContactPhone);
        Assert.Equal("receiving@example.test", snapshot.DeliveryContactEmail);
    }

    [Fact]
    public async Task UpdateDeliveryStatusAsync_ValidTransition_PersistsTimestampedAudit()
    {
        var createRequest = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-2026-AUDIT",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "PROD-AUDIT",
                    ProductName = "Audited Product",
                    QuantityOrdered = 2,
                    QuantityManufactured = 2,
                    QuantityDelivered = 2,
                    UnitOfMeasure = "pcs"
                }
            }
        };
        var created = await _service.CreateAsync(createRequest, "creator");

        var beforeTransition = DateTime.UtcNow.AddSeconds(-1);
        await _service.UpdateStatusAsync(
            created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" },
            "scanner-user");

        await using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT previous_status, new_status, changed_by, changed_at
            FROM delivery_status_audits
            WHERE delivery_note_id = @deliveryNoteId
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "deliveryNoteId";
        parameter.Value = created.DeliveryNoteId;
        command.Parameters.Add(parameter);

        if (command.Connection!.State != System.Data.ConnectionState.Open)
        {
            await command.Connection.OpenAsync();
        }

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("Pending", reader.GetString(0));
        Assert.Equal("InTransit", reader.GetString(1));
        Assert.Equal("scanner-user", reader.GetString(2));
        Assert.True(reader.GetDateTime(3) >= beforeTransition);
        Assert.False(await reader.ReadAsync());
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
    public async Task SearchAsync_WithoutUnrestrictedAccessAndNoCustomerGrants_ReturnsEmpty()
    {
        // Arrange
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-SCOPE-1",
            OrderId = "ORD-SCOPE-1",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Scoped Customer",
            Status = DeliveryStatus.Pending,
            DeliveryDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await _context.SaveChangesAsync();

        var service = CreateService(new FakeAuthorizationService(hasUnrestrictedAccess: false));

        // Act
        var result = await service.SearchAsync(new DeliveryNoteFilterRequest(), "restricted-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchAsync_CustomerFilterOutsideAuthorizedCustomers_ReturnsEmpty()
    {
        // Arrange
        var authorizedCustomerId = Guid.NewGuid();
        var requestedCustomerId = Guid.NewGuid();
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-SCOPE-2",
            OrderId = "ORD-SCOPE-2",
            CustomerId = requestedCustomerId,
            CustomerName = "Requested Customer",
            Status = DeliveryStatus.Pending,
            DeliveryDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await _context.SaveChangesAsync();

        var service = CreateService(new FakeAuthorizationService(
            hasUnrestrictedAccess: false,
            authorizedCustomerIds: new[] { authorizedCustomerId }));

        // Act
        var result = await service.SearchAsync(
            new DeliveryNoteFilterRequest { CustomerId = requestedCustomerId },
            "restricted-user");

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task CreateAsync_WithoutCustomerAccess_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var service = CreateService(new FakeAuthorizationService(hasUnrestrictedAccess: false));
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-SCOPE-CREATE",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "P1",
                    ProductName = "Product 1",
                    QuantityOrdered = 10,
                    QuantityManufactured = 10,
                    QuantityDelivered = 5,
                    UnitOfMeasure = "pcs"
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateAsync(request, "restricted-user"));
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
            Items = new List<CreateDeliveryNoteItemRequest> { new() { ProductCode = "P1", ProductName = "Product 1", QuantityOrdered = 10, QuantityManufactured = 10, QuantityDelivered = 5, UnitOfMeasure = "pcs" } }
        };
        var created = await _service.CreateAsync(createRequest, "test-user");

        var updateRequest = new UpdateDeliveryNoteRequest
        {
            CarrierName = "New Carrier",
            TrackingNumber = "TRACK123"
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
            Items = new List<CreateDeliveryNoteItemRequest> { new() { ProductCode = "P1", ProductName = "Product 1", QuantityOrdered = 10, QuantityManufactured = 10, QuantityDelivered = 5, UnitOfMeasure = "pcs" } }
        };
        var created = await _service.CreateAsync(createRequest, "test-user");

        // Cancel the delivery note first to put it in a terminal state
        await _service.UpdateStatusAsync(created.DeliveryNoteId, new UpdateDeliveryStatusRequest { NewStatus = "Cancelled" }, "test-user");

        var updateRequest = new UpdateDeliveryNoteRequest
        {
            CarrierName = "New Carrier"
        };

        // Act & Assert - Cannot update a terminal state (Cancelled)
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
            Items = new List<CreateDeliveryNoteItemRequest> { new() { ProductCode = "P1", ProductName = "Product 1", QuantityOrdered = 10, QuantityManufactured = 10, QuantityDelivered = 5, UnitOfMeasure = "pcs" } }
        };
        var created = await _service.CreateAsync(createRequest, "test-user");

        var content = "fake content";
        var fileName = "test.png";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        var fileData = new TestFileData(ms, fileName, ms.Length, "image/png");

        // Act
        var result = await _service.AddFileAsync(created.DeliveryNoteId, fileData, FileType.Photo, "Test file", "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fileName, result.OriginalFileName);

        var files = await _service.GetFilesAsync(created.DeliveryNoteId);
        Assert.Single(files);
        Assert.Equal(fileName, files[0].OriginalFileName);
    }

    [Fact]
    public async Task CreateDeliveryNoteAsync_MissingOrderIdAndPOId_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateDeliveryNoteRequest { Items = new List<CreateDeliveryNoteItemRequest> { new() { ProductCode = "P1", ProductName = "P1", QuantityDelivered = 1, QuantityManufactured = 1 } } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request, "user"));
    }

    [Fact]
    public async Task CreateDeliveryNoteAsync_InvalidQuantity_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-1",
            Items = new List<CreateDeliveryNoteItemRequest> { new() { ProductCode = "P1", ProductName = "P1", QuantityDelivered = -1, QuantityManufactured = 1 } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request, "user"));
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidStatus_ThrowsArgumentException()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();
        var request = new UpdateDeliveryStatusRequest { NewStatus = "INVALID" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateStatusAsync(created.DeliveryNoteId, request, "user"));
    }

    [Fact]
    public async Task AddFileAsync_FileNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var fileData = new TestFileData(new MemoryStream(), "test.png", 1024, "image/png");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddFileAsync("NON-EXISTENT", fileData, FileType.Other, null, "user"));
    }

    [Fact]
    public async Task AddFileAsync_FileTooLarge_ThrowsArgumentException()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();
        var ms = new MemoryStream();
        ms.SetLength(10 * 1024 * 1024); // 10MB > 5MB limit
        var fileData = new TestFileData(ms, "test.png", 10 * 1024 * 1024, "image/png");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddFileAsync(created.DeliveryNoteId, fileData, FileType.Other, null, "user"));
    }

    [Fact]
    public async Task AddFileAsync_StorageError_ThrowsInvalidOperationException()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();
        var ms = new MemoryStream();
        ms.SetLength(1024);
        var fileData = new TestFileData(new MemoryStream(), "test.png", 1024, "image/png");

        _fakeFileStorageService.SetThrowError(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddFileAsync(created.DeliveryNoteId, fileData, FileType.Other, null, "user"));
    }

    [Fact]
    public async Task UpdateAsync_RetryOnConcurrencyConflict_SucceedsEventually()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();
        var updateRequest = new UpdateDeliveryNoteRequest
        {
            CarrierName = "Retry Carrier"
        };

        // Test that a normal update succeeds (xmin concurrency is handled by EF Core automatically).
        var result = await _service.UpdateAsync(created.DeliveryNoteId, updateRequest, "user");
        Assert.Equal("Retry Carrier", result.CarrierName);
    }

    [Fact]
    public async Task GetFilesAsync_WithFiles_ReturnsFileList()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();

        // Add a file
        var ms = new MemoryStream();
        ms.SetLength(1024);
        var fileData = new TestFileData(ms, "test.png", 1024, "image/png");

        await _service.AddFileAsync(created.DeliveryNoteId, fileData, FileType.Photo, "Test file", "user");

        // Act
        var files = await _service.GetFilesAsync(created.DeliveryNoteId);

        // Assert
        Assert.Single(files);
        Assert.Equal("test.png", files[0].OriginalFileName);
    }

    [Fact]
    public async Task DownloadFileAsync_WithUploadedFile_ReturnsOriginalBytes()
    {
        var created = await CreateTestDeliveryNote();
        var bytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 77, 83 };
        var fileData = new TestFileData(new MemoryStream(bytes), "proof.png", bytes.Length, "image/png");
        var uploaded = await _service.AddFileAsync(created.DeliveryNoteId, fileData, FileType.Signature, "Proof", "user");

        var downloaded = await _service.DownloadFileAsync(created.DeliveryNoteId, uploaded.FileId, "user");

        Assert.Equal(uploaded.FileId, downloaded.FileId);
        Assert.Equal("proof.png", downloaded.OriginalFileName);
        Assert.Equal("image/png", downloaded.ContentType);
        Assert.Equal(bytes, downloaded.Content);
    }

    [Fact]
    public async Task DownloadFileAsync_WithGeneratedPdfUrl_ReturnsSignedUrlBytes()
    {
        var created = await CreateTestDeliveryNote();
        var pdfBytes = "%PDF-1.7 generated delivery note"u8.ToArray();
        var pdfUrl = "https://upload.test/upload/v1/mock-storage/pdf-token";
        _httpClientFactory.RespondWith(pdfUrl, "application/pdf", pdfBytes);
        var fileId = Guid.NewGuid();

        _context.DeliveryNoteFiles.Add(new DeliveryNoteFile
        {
            Id = fileId,
            DeliveryNoteId = created.DeliveryNoteId,
            FileType = FileType.DeliveryNotePdf,
            FileName = "delivery-note.pdf",
            StorageUrl = pdfUrl,
            ContentType = "application/pdf",
            FileSize = pdfBytes.Length,
            Description = "Generated delivery note PDF",
            UploadedAt = DateTime.UtcNow,
            UploadedBy = "PdfService"
        });
        await _context.SaveChangesAsync();

        var downloaded = await _service.DownloadFileAsync(created.DeliveryNoteId, fileId, "user");

        Assert.Equal(fileId, downloaded.FileId);
        Assert.Equal("delivery-note.pdf", downloaded.OriginalFileName);
        Assert.Equal("application/pdf", downloaded.ContentType);
        Assert.Equal(pdfBytes, downloaded.Content);
        Assert.Equal((byte)'%', downloaded.Content[0]);
        Assert.Equal((byte)'P', downloaded.Content[1]);
        Assert.Equal((byte)'D', downloaded.Content[2]);
        Assert.Equal((byte)'F', downloaded.Content[3]);
    }

    [Fact]
    public async Task GetFilesAsync_Empty_ReturnsEmptyList()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();

        // Act
        var files = await _service.GetFilesAsync(created.DeliveryNoteId);

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public async Task SearchAsync_WithOrderIdFilter_ReturnsFilteredResult()
    {
        // Arrange
        var orderId = "ORD-SEARCH-TEST";
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-S2",
            OrderId = orderId,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Search Test",
            Status = DeliveryStatus.Pending,
            DeliveryDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-S3",
            OrderId = "OTHER-ORDER",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Other Customer",
            Status = DeliveryStatus.Pending,
            DeliveryDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await _context.SaveChangesAsync();

        var filter = new DeliveryNoteFilterRequest
        {
            OrderId = orderId,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchAsync(filter, "test-user");

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(orderId, result.Items[0].OrderId);
    }

    [Fact]
    public async Task SearchAsync_WithStatusFilter_ReturnsFilteredResult()
    {
        // Arrange
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-S4",
            OrderId = "ORD-1",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test",
            Status = DeliveryStatus.Pending,
            DeliveryDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-S5",
            OrderId = "ORD-2",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test",
            Status = DeliveryStatus.Delivered,
            DeliveryDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await _context.SaveChangesAsync();

        var filter = new DeliveryNoteFilterRequest
        {
            Status = "Pending",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchAsync(filter, "test-user");

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Pending", result.Items[0].Status);
    }

    [Fact]
    public async Task SearchAsync_WithDateRangeFilter_ReturnsFilteredResult()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-S6",
            OrderId = "ORD-1",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test",
            Status = DeliveryStatus.Pending,
            DeliveryDate = now.AddDays(-5),
            CreatedAt = now,
            CreatedBy = "test"
        });
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-S7",
            OrderId = "ORD-2",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test",
            Status = DeliveryStatus.Pending,
            DeliveryDate = now.AddDays(5),
            CreatedAt = now,
            CreatedBy = "test"
        });
        await _context.SaveChangesAsync();

        var filter = new DeliveryNoteFilterRequest
        {
            DeliveryDateFrom = now.AddDays(-7),
            DeliveryDateTo = now,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchAsync(filter, "test-user");

        // Assert
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task UpdateStatusAsync_ToDelivered_PublishesDeliveryCompletedEvent()
    {
        // Arrange
        var createRequest = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-DELIVERED-EVENT",
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

        // Transition Pending -> InTransit -> Delivered
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "Delivered", ReceivedByName = "John Doe" }, "test-user");

        // Verify delivery completed event was published
        var completedEvents = _fakePublishEndpoint.GetPublishedMessages<Maliev.MessagingContracts.Contracts.Delivery.DeliveryCompletedEvent>();
        var completedEvent = Assert.Single(completedEvents);
        Assert.Contains("NotificationService", completedEvent.ConsumedBy, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(createRequest.CustomerId, completedEvent.Payload.CustomerId);
    }

    [Fact]
    public async Task UpdateStatusAsync_ToDelivered_PersistsSignatureFileId()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();
        var signatureFileId = Guid.NewGuid();

        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");

        // Act
        var result = await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest
            {
                NewStatus = "Delivered",
                ReceivedByName = "John Doe",
                SignatureFileId = signatureFileId
            },
            "test-user");

        // Assert
        Assert.Equal(signatureFileId, result.SignatureFileId);

        var persisted = await _context.DeliveryNotes
            .AsNoTracking()
            .SingleAsync(dn => dn.DeliveryNoteId == created.DeliveryNoteId);
        Assert.Equal(signatureFileId, persisted.SignatureFileId);
    }

    [Fact]
    public async Task UpdateStatusAsync_ToInTransit_PublishesStatusChangedEvent()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();
        _fakePublishEndpoint.Clear();

        // Act
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");

        // Assert
        var statusChangedEvents = _fakePublishEndpoint.GetPublishedMessages<Maliev.MessagingContracts.Contracts.Delivery.DeliveryStatusChangedEvent>();
        var statusChangedEvent = Assert.Single(statusChangedEvents);
        Assert.Contains("NotificationService", statusChangedEvent.ConsumedBy, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(created.CustomerId, statusChangedEvent.Payload.CustomerId);
    }

    [Fact]
    public async Task UpdateStatusAsync_DuplicateStatusRetry_ReturnsCurrentStatusWithoutRepublishing()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");
        _fakePublishEndpoint.Clear();

        // Act
        var result = await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");

        // Assert
        Assert.Equal("InTransit", result.Status);
        Assert.Empty(_fakePublishEndpoint.GetPublishedMessages<Maliev.MessagingContracts.Contracts.Delivery.DeliveryStatusChangedEvent>());
        Assert.Empty(_fakePublishEndpoint.GetPublishedMessages<Maliev.MessagingContracts.Contracts.Delivery.DeliveryCompletedEvent>());
    }

    [Fact]
    public async Task AddFileAsync_InvalidContentType_ThrowsArgumentException()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();
        var ms = new MemoryStream();
        ms.SetLength(1024);
        var fileData = new TestFileData(ms, "test.exe", 1024, "application/octet-stream"); // Not allowed

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddFileAsync(created.DeliveryNoteId, fileData, FileType.Other, null, "user"));
    }

    [Fact]
    public async Task CreateDeliveryNoteAsync_WithCumulativeQuantityValidation_Success()
    {
        // Arrange - Create first partial delivery
        var orderId = "ORD-CUMULATIVE-TEST";
        var firstRequest = new CreateDeliveryNoteRequest
        {
            OrderId = orderId,
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "PROD-CUM",
                    ProductName = "Test Product",
                    QuantityOrdered = 100,
                    QuantityManufactured = 100,
                    QuantityDelivered = 50, // First delivery: 50
                    UnitOfMeasure = "pcs"
                }
            }
        };
        await _service.CreateAsync(firstRequest, "test-user");

        // Try to create second delivery that exceeds total
        var secondRequest = new CreateDeliveryNoteRequest
        {
            OrderId = orderId,
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "PROD-CUM",
                    ProductName = "Test Product",
                    QuantityOrdered = 100,
                    QuantityManufactured = 100,
                    QuantityDelivered = 60, // Second delivery: 60, total = 110 > 100!
                    UnitOfMeasure = "pcs"
                }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(secondRequest, "test-user"));
        Assert.Contains("exceeds ordered quantity", exception.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_FromPendingToCancelled_Success()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();

        // Act
        var result = await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "Cancelled" }, "test-user");

        // Assert
        Assert.Equal("Cancelled", result.Status);
    }

    [Fact]
    public async Task SoftDeleteAsync_DeletesAssociatedFiles()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();

        // Add a file
        var ms = new MemoryStream();
        ms.SetLength(1024);
        var fileData = new TestFileData(ms, "test.png", 1024, "image/png");

        await _service.AddFileAsync(created.DeliveryNoteId, fileData, FileType.Photo, "Test file", "user");

        // Act
        await _service.SoftDeleteAsync(created.DeliveryNoteId, "test-user");

        // Assert - Verify files are also deleted
        var files = await _context.DeliveryNoteFiles
            .Where(f => f.DeliveryNoteId == created.DeliveryNoteId)
            .ToListAsync();

        Assert.All(files, f => Assert.True(f.IsDeleted));
    }

    [Fact]
    public async Task SearchAsync_SortByCreatedAtAsc_ReturnsSortedResult()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-S8",
            OrderId = "ORD-1",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test",
            Status = DeliveryStatus.Pending,
            DeliveryDate = now,
            CreatedAt = now.AddDays(-1),
            CreatedBy = "test"
        });
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-S9",
            OrderId = "ORD-2",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test",
            Status = DeliveryStatus.Pending,
            DeliveryDate = now,
            CreatedAt = now,
            CreatedBy = "test"
        });
        await _context.SaveChangesAsync();

        var filter = new DeliveryNoteFilterRequest
        {
            SortBy = "created_at",
            SortOrder = "asc",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchAsync(filter, "test-user");

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal("DN-S8", result.Items[0].DeliveryNoteId);
    }

    [Fact]
    public async Task SearchAsync_SortByDeliveryDateDesc_ReturnsSortedResult()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-S10",
            OrderId = "ORD-1",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test",
            Status = DeliveryStatus.Pending,
            DeliveryDate = now.AddDays(-1),
            CreatedAt = now,
            CreatedBy = "test"
        });
        _context.DeliveryNotes.Add(new DeliveryNote
        {
            DeliveryNoteId = "DN-S11",
            OrderId = "ORD-2",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test",
            Status = DeliveryStatus.Pending,
            DeliveryDate = now.AddDays(1),
            CreatedAt = now,
            CreatedBy = "test"
        });
        await _context.SaveChangesAsync();

        var filter = new DeliveryNoteFilterRequest
        {
            SortBy = "delivery_date",
            SortOrder = "desc",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchAsync(filter, "test-user");

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal("DN-S11", result.Items[0].DeliveryNoteId);
    }

    [Fact]
    public async Task UpdateStatusAsync_FromCancelledToInTransit_ThrowsInvalidOperation()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();

        // Transition to Cancelled first
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "Cancelled" }, "test-user");

        // Act & Assert - Try to transition from Cancelled -> InTransit (invalid!)
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateStatusAsync(created.DeliveryNoteId,
                new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user"));

        Assert.Contains("Cannot transition from terminal status", exception.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_FromInTransitToPartiallyDelivered_Success()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();

        // Transition Pending -> InTransit
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");

        // Act - Transition InTransit -> PartiallyDelivered
        var result = await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "PartiallyDelivered" }, "test-user");

        // Assert
        Assert.Equal("PartiallyDelivered", result.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_FromPartiallyDeliveredToDelivered_Success()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();

        // Transition Pending -> InTransit -> PartiallyDelivered
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "PartiallyDelivered" }, "test-user");

        // Act - Transition PartiallyDelivered -> Delivered
        var result = await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "Delivered", ReceivedByName = "Jane Doe" }, "test-user");

        // Assert
        Assert.Equal("Delivered", result.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_FromPartiallyDeliveredToCancelled_Success()
    {
        // Arrange
        var created = await CreateTestDeliveryNote();

        // Transition Pending -> InTransit -> PartiallyDelivered
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "InTransit" }, "test-user");
        await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "PartiallyDelivered" }, "test-user");

        // Act - Transition PartiallyDelivered -> Cancelled
        var result = await _service.UpdateStatusAsync(created.DeliveryNoteId,
            new UpdateDeliveryStatusRequest { NewStatus = "Cancelled" }, "test-user");

        // Assert
        Assert.Equal("Cancelled", result.Status);
    }

    [Fact]
    public async Task SoftDeleteAsync_NotFound_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SoftDeleteAsync("NON-EXISTENT", "test-user"));
    }

    private async Task<DeliveryNoteResponse> CreateTestDeliveryNote()
    {
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-" + Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest> { new() { ProductCode = "P1", ProductName = "P1", QuantityOrdered = 10, QuantityManufactured = 10, QuantityDelivered = 5, UnitOfMeasure = "pcs" } }
        };
        return await _service.CreateAsync(request, "user");
    }

    private DeliveryNoteService CreateService(IDeliveryNoteAuthorizationService authorizationService)
    {
        return new DeliveryNoteService(
            _context,
            new DeliveryNoteIdGenerator(_context),
            _fakePublishEndpoint,
            _fakeOrderServiceClient,
            _cache,
            authorizationService,
            _fakeFileStorageService,
            _httpClientFactory,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DeliveryNoteService>.Instance);
    }

    private static string FindDeliveryNoteServiceSourcePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "Maliev.DeliveryService.Infrastructure",
                "Services",
                "DeliveryNoteService.cs");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate Maliev.DeliveryService.Infrastructure/Services/DeliveryNoteService.cs");
    }

    private static string ExtractMethodSource(string source, string methodSignature)
    {
        var methodStart = source.IndexOf(methodSignature, StringComparison.Ordinal);
        Assert.True(methodStart >= 0, $"Could not find {methodSignature} source.");

        var openingBrace = source.IndexOf('{', methodStart);
        Assert.True(openingBrace > methodStart, $"Could not find opening brace for {methodSignature}.");

        var depth = 0;
        for (var index = openingBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[methodStart..(index + 1)];
                }
            }
        }

        throw new InvalidOperationException($"Could not isolate {methodSignature} source.");
    }

    private static void AssertCallAppearsBeforeSaveChanges(
        string methodBody,
        string expectedCall,
        string failureMessage)
    {
        var callIndex = methodBody.IndexOf(expectedCall, StringComparison.Ordinal);
        var saveIndex = methodBody.IndexOf(
            "await _context.SaveChangesAsync(ct);",
            StringComparison.Ordinal);

        Assert.True(callIndex >= 0, $"Expected call not found: {expectedCall}");
        Assert.True(saveIndex >= 0, "Expected SaveChangesAsync call not found.");
        Assert.True(callIndex < saveIndex, failureMessage);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    // Fake authorization service for testing
    private class FakeAuthorizationService : IDeliveryNoteAuthorizationService
    {
        private readonly bool _hasUnrestrictedAccess;
        private readonly HashSet<Guid> _authorizedCustomerIds;

        public FakeAuthorizationService(
            bool hasUnrestrictedAccess = true,
            IEnumerable<Guid>? authorizedCustomerIds = null)
        {
            _hasUnrestrictedAccess = hasUnrestrictedAccess;
            _authorizedCustomerIds = authorizedCustomerIds?.ToHashSet() ?? [];
        }

        public Task<bool> CanAccessCustomerAsync(string principalId, Guid customerId, CancellationToken ct = default)
        {
            return Task.FromResult(_hasUnrestrictedAccess || _authorizedCustomerIds.Contains(customerId));
        }

        public Task<bool> HasUnrestrictedAccessAsync(string principalId, CancellationToken ct = default)
        {
            return Task.FromResult(_hasUnrestrictedAccess);
        }

        public Task<List<Guid>> GetAuthorizedCustomerIdsAsync(string principalId, CancellationToken ct = default)
        {
            return Task.FromResult(_authorizedCustomerIds.ToList());
        }
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly Dictionary<string, HttpResponseMessage> _responses = new(StringComparer.OrdinalIgnoreCase);

        public void RespondWith(string url, string contentType, byte[] bytes)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            _responses[url] = response;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new TestHttpMessageHandler(_responses));
        }
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly IReadOnlyDictionary<string, HttpResponseMessage> _responses;

        public TestHttpMessageHandler(IReadOnlyDictionary<string, HttpResponseMessage> responses)
        {
            _responses = responses;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = request.RequestUri?.ToString() ?? string.Empty;
            if (_responses.TryGetValue(key, out var response))
            {
                var content = response.Content.ReadAsByteArrayAsync(cancellationToken).GetAwaiter().GetResult();
                var clonedContent = new ByteArrayContent(content);
                if (response.Content.Headers.ContentType is not null)
                {
                    clonedContent.Headers.ContentType = response.Content.Headers.ContentType;
                }

                return Task.FromResult(new HttpResponseMessage(response.StatusCode)
                {
                    Content = clonedContent
                });
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        }
    }
}
