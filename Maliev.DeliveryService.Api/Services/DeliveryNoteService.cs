using Maliev.DeliveryService.Api.Clients;
using Maliev.DeliveryService.Api.DTOs;
using Maliev.DeliveryService.Data;
using Maliev.DeliveryService.Data.Entities;
using Maliev.MessagingContracts.Contracts.Delivery;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Maliev.DeliveryService.Api.Services;

/// <summary>
/// Service for managing delivery notes.
/// </summary>
public class DeliveryNoteService : IDeliveryNoteService
{
    private readonly DeliveryDbContext _context;
    private readonly DeliveryNoteIdGenerator _idGenerator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IOrderServiceClient _orderServiceClient;
    private readonly IDistributedCache _cache;
    private readonly IDeliveryNoteAuthorizationService _authorizationService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<DeliveryNoteService> _logger;

    // File validation constants
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    private static readonly HashSet<string> AllowedMimeTypes = new()
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "application/pdf"
    };

    /// <summary>
    /// Initializes a new instance of the DeliveryNoteService class.
    /// </summary>
    public DeliveryNoteService(
        DeliveryDbContext context,
        DeliveryNoteIdGenerator idGenerator,
        IPublishEndpoint publishEndpoint,
        IOrderServiceClient orderServiceClient,
        IDistributedCache cache,
        IDeliveryNoteAuthorizationService authorizationService,
        IFileStorageService fileStorageService,
        ILogger<DeliveryNoteService> logger)
    {
        _context = context;
        _idGenerator = idGenerator;
        _publishEndpoint = publishEndpoint;
        _orderServiceClient = orderServiceClient;
        _cache = cache;
        _authorizationService = authorizationService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new delivery note.
    /// </summary>
    public async Task<DeliveryNoteResponse> CreateAsync(CreateDeliveryNoteRequest request, string createdBy, CancellationToken ct = default)
    {
        // Validate request
        ValidateCreateRequest(request);

        // Validate cumulative quantities for partial deliveries (if OrderId is provided)
        if (!string.IsNullOrEmpty(request.OrderId))
        {
            await ValidateCumulativeQuantitiesAsync(request, ct);
        }

        // Generate sequential ID
        var deliveryNoteId = await _idGenerator.GenerateNextIdAsync(ct);

        // Create entity
        var deliveryNote = request.ToEntity(deliveryNoteId, createdBy);

        // Save to database
        _context.DeliveryNotes.Add(deliveryNote);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Delivery note created: {DeliveryNoteId}, OrderId: {OrderId}, CustomerId: {CustomerId}, CreatedBy: {CreatedBy}",
            deliveryNote.DeliveryNoteId, deliveryNote.OrderId, deliveryNote.CustomerId, createdBy);

        // Invalidate cache for this delivery note (in case it was previously cached and deleted)
        var cacheKey = $"delivery-note:{deliveryNote.DeliveryNoteId}";
        await _cache.RemoveAsync(cacheKey, ct);

        // Publish DeliveryNoteCreatedEvent with graceful degradation
        await PublishEventAsync(new DeliveryNoteCreatedEvent
        {
            Payload = new DeliveryNoteCreatedEventPayload
            {
                DeliveryNoteId = deliveryNote.DeliveryNoteId,
                OrderId = deliveryNote.OrderId,
                PurchaseOrderId = deliveryNote.PurchaseOrderId,
                CustomerId = deliveryNote.CustomerId,
                DeliveryDate = deliveryNote.DeliveryDate,
                ItemCount = deliveryNote.Items.Count,
                CreatedAt = deliveryNote.CreatedAt,
                CreatedBy = deliveryNote.CreatedBy
            }
        }, ct);

        return deliveryNote.ToResponse();
    }

    /// <summary>
    /// Gets a delivery note by its ID.
    /// </summary>
    public async Task<DeliveryNoteResponse?> GetByIdAsync(string deliveryNoteId, CancellationToken ct = default)
    {
        // Try to get from cache first
        var cacheKey = $"delivery-note:{deliveryNoteId}";
        var cachedValue = await _cache.GetStringAsync(cacheKey, ct);

        if (!string.IsNullOrEmpty(cachedValue))
        {
            _logger.LogDebug("Cache hit for delivery note {DeliveryNoteId}", deliveryNoteId);
            return JsonSerializer.Deserialize<DeliveryNoteResponse>(cachedValue);
        }

        var deliveryNote = await _context.DeliveryNotes
            .Include(dn => dn.Items)
            .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == deliveryNoteId && !dn.IsDeleted, ct);

        if (deliveryNote == null)
        {
            return null;
        }

        var response = deliveryNote.ToResponse();

        // Cache for 5 minutes
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), cacheOptions, ct);

        return response;
    }

    /// <summary>
    /// Searches for delivery notes based on the provided filter.
    /// </summary>
    public async Task<PaginatedResponse<DeliveryNoteSummaryDto>> SearchAsync(
        DeliveryNoteFilterRequest filter,
        string principalId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Searching delivery notes: OrderId={OrderId}, CustomerId={CustomerId}, DateFrom={DateFrom}, DateTo={DateTo}, Status={Status}, Page={Page}, PageSize={PageSize}",
            filter.OrderId, filter.CustomerId, filter.DeliveryDateFrom, filter.DeliveryDateTo, filter.Status, filter.Page, filter.PageSize);

        // Get authorized customer IDs for customer-scoped filtering
        var authorizedCustomerIds = await _authorizationService.GetAuthorizedCustomerIdsAsync(principalId, ct);

        var query = _context.DeliveryNotes.AsQueryable().Where(dn => !dn.IsDeleted);

        // Apply customer-scoped filter (if user has customer restrictions)
        if (authorizedCustomerIds.Any())
        {
            query = query.Where(dn => authorizedCustomerIds.Contains(dn.CustomerId));
        }

        // Apply filters
        if (!string.IsNullOrEmpty(filter.OrderId))
        {
            query = query.Where(dn => dn.OrderId == filter.OrderId);
        }

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(dn => dn.CustomerId == filter.CustomerId.Value);
        }

        if (filter.DeliveryDateFrom.HasValue)
        {
            query = query.Where(dn => dn.DeliveryDate >= filter.DeliveryDateFrom.Value);
        }

        if (filter.DeliveryDateTo.HasValue)
        {
            query = query.Where(dn => dn.DeliveryDate <= filter.DeliveryDateTo.Value);
        }

        if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<DeliveryStatus>(filter.Status, ignoreCase: true, out var status))
        {
            query = query.Where(dn => dn.Status == status);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(ct);

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "created_at" => filter.SortOrder?.ToLower() == "asc"
                ? query.OrderBy(dn => dn.CreatedAt)
                : query.OrderByDescending(dn => dn.CreatedAt),
            "delivery_date" or _ => filter.SortOrder?.ToLower() == "asc"
                ? query.OrderBy(dn => dn.DeliveryDate)
                : query.OrderByDescending(dn => dn.DeliveryDate)
        };

        // Apply pagination
        var pageSize = Math.Min(filter.PageSize, 100); // Max 100 per page
        var items = await query
            .Skip((filter.Page - 1) * pageSize)
            .Take(pageSize)
            .Select(dn => new DeliveryNoteSummaryDto
            {
                DeliveryNoteId = dn.DeliveryNoteId,
                OrderId = dn.OrderId,
                CustomerId = dn.CustomerId,
                CustomerName = dn.CustomerName,
                DeliveryDate = dn.DeliveryDate,
                Status = dn.Status.ToString(),
                ItemCount = dn.Items.Count,
                CarrierName = dn.CarrierName,
                TrackingNumber = dn.TrackingNumber,
                CreatedAt = dn.CreatedAt
            })
            .ToListAsync(ct);

        _logger.LogInformation(
            "Search completed: Found {TotalCount} delivery notes, returning page {Page}/{TotalPages} ({ItemCount} items)",
            totalCount, filter.Page, (int)Math.Ceiling(totalCount / (double)pageSize), items.Count);

        return new PaginatedResponse<DeliveryNoteSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Updates the status of an existing delivery note.
    /// </summary>
    public async Task<DeliveryNoteResponse> UpdateStatusAsync(
        string deliveryNoteId,
        UpdateDeliveryStatusRequest request,
        string updatedBy,
        CancellationToken ct = default)
    {
        var deliveryNote = await _context.DeliveryNotes
            .Include(dn => dn.Items)
            .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == deliveryNoteId && !dn.IsDeleted, ct);

        if (deliveryNote == null)
        {
            throw new InvalidOperationException($"Delivery note {deliveryNoteId} not found");
        }

        // Parse and validate status transition
        if (!Enum.TryParse<DeliveryStatus>(request.NewStatus, ignoreCase: true, out var newStatus))
        {
            throw new ArgumentException($"Invalid status: {request.NewStatus}");
        }

        ValidateStatusTransition(deliveryNote.Status, newStatus, request);

        var oldStatus = deliveryNote.Status;
        deliveryNote.Status = newStatus;
        deliveryNote.UpdatedAt = DateTime.UtcNow;
        deliveryNote.UpdatedBy = updatedBy;

        // Set delivery confirmation fields if status is Delivered
        if (newStatus == DeliveryStatus.Delivered)
        {
            deliveryNote.ActualDeliveryTime = request.ActualDeliveryTime ?? DateTime.UtcNow;
            deliveryNote.ReceivedByName = request.ReceivedByName;
            deliveryNote.SignedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Delivery note status updated: {DeliveryNoteId}, Status: {OldStatus} -> {NewStatus}, UpdatedBy: {UpdatedBy}",
            deliveryNoteId, oldStatus, newStatus, updatedBy);

        // Invalidate cache for this delivery note
        var cacheKey = $"delivery-note:{deliveryNoteId}";
        await _cache.RemoveAsync(cacheKey, ct);

        // Publish DeliveryStatusChangedEvent
        await PublishEventAsync(new DeliveryStatusChangedEvent
        {
            Payload = new DeliveryStatusChangedEventPayload
            {
                DeliveryNoteId = deliveryNote.DeliveryNoteId,
                OrderId = deliveryNote.OrderId,
                PreviousStatus = oldStatus.ToString(),
                NewStatus = newStatus.ToString(),
                ActualDeliveryTime = deliveryNote.ActualDeliveryTime,
                ReceivedByName = deliveryNote.ReceivedByName,
                ChangedAt = deliveryNote.UpdatedAt ?? DateTime.UtcNow,
                ChangedBy = updatedBy
            }
        }, ct);

        // Publish DeliveryCompletedEvent when fully delivered
        if (newStatus == DeliveryStatus.Delivered)
        {
            await PublishEventAsync(new DeliveryCompletedEvent
            {
                Payload = new DeliveryCompletedEventPayload
                {
                    DeliveryNoteId = deliveryNote.DeliveryNoteId,
                    OrderId = deliveryNote.OrderId,
                    PurchaseOrderId = deliveryNote.PurchaseOrderId,
                    CompletedAt = deliveryNote.ActualDeliveryTime ?? DateTime.UtcNow,
                    ReceivedByName = deliveryNote.ReceivedByName ?? string.Empty
                }
            }, ct);
        }

        return deliveryNote.ToResponse();
    }

    /// <summary>
    /// Adds a file to a delivery note.
    /// </summary>
    public async Task<DeliveryNoteFileResponse> AddFileAsync(
        string deliveryNoteId,
        IFormFile file,
        Data.Entities.FileType fileType,
        string? description,
        string uploadedBy,
        CancellationToken ct = default)
    {
        // Validate delivery note exists
        var deliveryNote = await _context.DeliveryNotes
            .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == deliveryNoteId && !dn.IsDeleted, ct);

        if (deliveryNote == null)
        {
            throw new InvalidOperationException($"Delivery note {deliveryNoteId} not found");
        }

        // Validate file size
        if (file.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024}MB");
        }

        // Validate file type
        if (!AllowedMimeTypes.Contains(file.ContentType))
        {
            throw new ArgumentException($"File type {file.ContentType} is not allowed. Allowed types: {string.Join(", ", AllowedMimeTypes)}");
        }

        try
        {
            // Upload to GCS
            using var stream = file.OpenReadStream();
            var storageUrl = await _fileStorageService.UploadAsync(stream, file.FileName, file.ContentType, ct);

            // Create file entity
            var deliveryNoteFile = new Data.Entities.DeliveryNoteFile
            {
                Id = Guid.NewGuid(),
                DeliveryNoteId = deliveryNoteId,
                FileType = fileType,
                FileName = file.FileName,
                StorageUrl = storageUrl,
                ContentType = file.ContentType,
                FileSize = file.Length,
                Description = description,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = uploadedBy
            };

            _context.DeliveryNoteFiles.Add(deliveryNoteFile);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "File uploaded to delivery note: {DeliveryNoteId}, FileId: {FileId}, FileType: {FileType}, Size: {Size} bytes",
                deliveryNoteId, deliveryNoteFile.Id, fileType, file.Length);

            return deliveryNoteFile.ToResponse();
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to upload file to delivery note {DeliveryNoteId}", deliveryNoteId);
            throw new InvalidOperationException("Failed to upload file. Please try again later.", ex);
        }
    }

    /// <summary>
    /// Retrieves all files associated with a delivery note.
    /// </summary>
    public async Task<List<DeliveryNoteFileResponse>> GetFilesAsync(
        string deliveryNoteId,
        CancellationToken ct = default)
    {
        var files = await _context.DeliveryNoteFiles
            .Where(f => f.DeliveryNoteId == deliveryNoteId && !f.IsDeleted)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(ct);

        return files.Select(f => f.ToResponse()).ToList();
    }

    /// <summary>
    /// Updates an existing delivery note.
    /// </summary>
    public async Task<DeliveryNoteResponse> UpdateAsync(
        string deliveryNoteId,
        UpdateDeliveryNoteRequest request,
        string updatedBy,
        CancellationToken ct = default)
    {
        const int maxRetries = 3;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                var deliveryNote = await _context.DeliveryNotes
                    .Include(dn => dn.Items)
                    .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == deliveryNoteId && !dn.IsDeleted, ct);

                if (deliveryNote == null)
                {
                    throw new InvalidOperationException($"Delivery note {deliveryNoteId} not found");
                }

                // Cannot update terminal states
                if (deliveryNote.Status == Data.Entities.DeliveryStatus.Delivered ||
                    deliveryNote.Status == Data.Entities.DeliveryStatus.Cancelled)
                {
                    throw new InvalidOperationException($"Cannot update delivery note in terminal status {deliveryNote.Status}");
                }

                // Optimistic concurrency check
                if (deliveryNote.RowVersion != request.RowVersion)
                {
                    throw new DbUpdateConcurrencyException($"Delivery note {deliveryNoteId} has been modified by another user");
                }

                // Update fields
                deliveryNote.CarrierName = request.CarrierName ?? deliveryNote.CarrierName;
                deliveryNote.TrackingNumber = request.TrackingNumber ?? deliveryNote.TrackingNumber;
                deliveryNote.ShippingCost = request.ShippingCost ?? deliveryNote.ShippingCost;
                deliveryNote.ShippingCostCurrency = request.ShippingCostCurrency ?? deliveryNote.ShippingCostCurrency;
                deliveryNote.DeliveryContactName = request.DeliveryContactName ?? deliveryNote.DeliveryContactName;
                deliveryNote.DeliveryContactPhone = request.DeliveryContactPhone ?? deliveryNote.DeliveryContactPhone;
                deliveryNote.DeliveryContactEmail = request.DeliveryContactEmail ?? deliveryNote.DeliveryContactEmail;
                deliveryNote.DeliveryInstructions = request.DeliveryInstructions ?? deliveryNote.DeliveryInstructions;
                deliveryNote.InternalNotes = request.InternalNotes ?? deliveryNote.InternalNotes;
                deliveryNote.UpdatedAt = DateTime.UtcNow;
                deliveryNote.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Delivery note updated: {DeliveryNoteId}, UpdatedBy: {UpdatedBy}",
                    deliveryNoteId, updatedBy);

                // Invalidate cache
                var cacheKey = $"delivery-note:{deliveryNoteId}";
                await _cache.RemoveAsync(cacheKey, ct);

                return deliveryNote.ToResponse();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    _logger.LogWarning(ex,
                        "Concurrency conflict updating delivery note {DeliveryNoteId} after {RetryCount} retries",
                        deliveryNoteId, retryCount);
                    throw new InvalidOperationException(
                        "The delivery note has been modified by another user. Please refresh and try again.", ex);
                }

                _logger.LogInformation(
                    "Concurrency conflict detected for {DeliveryNoteId}, retrying ({RetryCount}/{MaxRetries})",
                    deliveryNoteId, retryCount, maxRetries);

                // Brief delay before retry
                await Task.Delay(TimeSpan.FromMilliseconds(100 * retryCount), ct);
            }
        }

        throw new InvalidOperationException("Update failed after maximum retries");
    }

    /// <summary>
    /// Soft deletes an existing delivery note.
    /// </summary>
    public async Task SoftDeleteAsync(string deliveryNoteId, string deletedBy, CancellationToken ct = default)
    {
        var deliveryNote = await _context.DeliveryNotes
            .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == deliveryNoteId && !dn.IsDeleted, ct);

        if (deliveryNote == null)
        {
            throw new InvalidOperationException($"Delivery note {deliveryNoteId} not found");
        }

        // Only allow deletion of Pending status
        if (deliveryNote.Status != Data.Entities.DeliveryStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot delete delivery note in status {deliveryNote.Status}. Only Pending delivery notes can be deleted.");
        }

        // Soft delete
        deliveryNote.IsDeleted = true;
        deliveryNote.DeletedAt = DateTime.UtcNow;
        deliveryNote.DeletedBy = deletedBy;

        // Also mark associated files as deleted
        var files = await _context.DeliveryNoteFiles
            .Where(f => f.DeliveryNoteId == deliveryNoteId && !f.IsDeleted)
            .ToListAsync(ct);

        foreach (var file in files)
        {
            file.IsDeleted = true;
            file.DeletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Delivery note soft deleted: {DeliveryNoteId}, DeletedBy: {DeletedBy}, FilesDeleted: {FileCount}",
            deliveryNoteId, deletedBy, files.Count);

        // Invalidate cache
        var cacheKey = $"delivery-note:{deliveryNoteId}";
        await _cache.RemoveAsync(cacheKey, ct);
    }

    private void ValidateCreateRequest(CreateDeliveryNoteRequest request)
    {
        // Must have either OrderId or PurchaseOrderId
        if (string.IsNullOrEmpty(request.OrderId) && !request.PurchaseOrderId.HasValue)
        {
            throw new ArgumentException("Either OrderId or PurchaseOrderId must be specified");
        }

        // Must have at least one item
        if (request.Items == null || !request.Items.Any())
        {
            throw new ArgumentException("At least one item is required");
        }

        // Validate each item
        foreach (var item in request.Items)
        {
            if (item.QuantityDelivered <= 0)
            {
                throw new ArgumentException($"Quantity delivered must be positive for product {item.ProductCode}");
            }

            if (item.QuantityDelivered > item.QuantityManufactured)
            {
                throw new ArgumentException(
                    $"Cannot deliver more than manufactured for product {item.ProductCode}. " +
                    $"Manufactured: {item.QuantityManufactured}, Attempted: {item.QuantityDelivered}");
            }
        }
    }

    private void ValidateStatusTransition(DeliveryStatus currentStatus, DeliveryStatus newStatus, UpdateDeliveryStatusRequest request)
    {
        // Cannot transition from terminal states
        if (currentStatus == DeliveryStatus.Delivered || currentStatus == DeliveryStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot transition from terminal status {currentStatus}");
        }

        // Valid transitions per state machine
        var validTransitions = currentStatus switch
        {
            DeliveryStatus.Pending => new[] { DeliveryStatus.InTransit, DeliveryStatus.Cancelled },
            DeliveryStatus.InTransit => new[] { DeliveryStatus.Delivered, DeliveryStatus.PartiallyDelivered, DeliveryStatus.Cancelled },
            DeliveryStatus.PartiallyDelivered => new[] { DeliveryStatus.Delivered, DeliveryStatus.Cancelled },
            _ => Array.Empty<DeliveryStatus>()
        };

        if (!validTransitions.Contains(newStatus))
        {
            throw new InvalidOperationException($"Invalid status transition: {currentStatus} -> {newStatus}");
        }

        // Delivered status requires ReceivedByName
        if (newStatus == DeliveryStatus.Delivered && string.IsNullOrWhiteSpace(request.ReceivedByName))
        {
            throw new ArgumentException("ReceivedByName is required when status is Delivered");
        }
    }

    private async Task ValidateCumulativeQuantitiesAsync(CreateDeliveryNoteRequest request, CancellationToken ct)
    {
        // Fetch existing delivery notes for the same order
        var existingDeliveries = await _context.DeliveryNotes
            .Include(dn => dn.Items)
            .Where(dn => dn.OrderId == request.OrderId && !dn.IsDeleted)
            .ToListAsync(ct);

        // Calculate cumulative delivered quantities per product
        foreach (var requestItem in request.Items)
        {
            if (string.IsNullOrEmpty(requestItem.ProductCode))
                continue;

            var totalDelivered = existingDeliveries
                .SelectMany(dn => dn.Items)
                .Where(item => item.ProductCode == requestItem.ProductCode)
                .Sum(item => item.QuantityDelivered);

            var totalWithCurrent = totalDelivered + requestItem.QuantityDelivered;

            if (totalWithCurrent > requestItem.QuantityOrdered)
            {
                throw new ArgumentException(
                    $"Total delivered quantity ({totalWithCurrent}) exceeds ordered quantity ({requestItem.QuantityOrdered}) " +
                    $"for product {requestItem.ProductCode}. " +
                    $"Previously delivered: {totalDelivered}, Attempting to deliver: {requestItem.QuantityDelivered}");
            }

            _logger.LogInformation(
                "Cumulative quantity check passed for {ProductCode}: Total={Total}, Current={Current}, Ordered={Ordered}",
                requestItem.ProductCode, totalWithCurrent, requestItem.QuantityDelivered, requestItem.QuantityOrdered);
        }
    }

    /// <summary>
    /// Scans a barcode for a delivery note and marks it as InTransit.
    /// </summary>
    public async Task<BarcodeScanResponse> ScanBarcodeAsync(
        string deliveryNoteId,
        string barcodeValue,
        string scannedBy,
        CancellationToken ct = default)
    {
        var trimmedBarcode = barcodeValue?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(trimmedBarcode))
            throw new ArgumentException("Barcode value must not be empty.");

        if (trimmedBarcode.Length > 100)
            throw new ArgumentException("Barcode value must not exceed 100 characters.");

        var deliveryNote = await _context.DeliveryNotes
            .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == deliveryNoteId && !dn.IsDeleted, ct);

        if (deliveryNote == null)
            throw new KeyNotFoundException($"Delivery note {deliveryNoteId} not found.");

        if (!await _authorizationService.CanAccessCustomerAsync(scannedBy, deliveryNote.CustomerId, ct))
        {
            throw new UnauthorizedAccessException($"User {scannedBy} is not authorized to access delivery note {deliveryNoteId}.");
        }

        if (deliveryNote.Status is DeliveryStatus.InTransit
                                or DeliveryStatus.Delivered
                                or DeliveryStatus.PartiallyDelivered
                                or DeliveryStatus.Cancelled)
        {
            throw new InvalidOperationException("This delivery note has already been dispatched.");
        }

        var previousStatus = deliveryNote.Status;

        deliveryNote.TrackingNumber = trimmedBarcode;
        deliveryNote.CarrierName = "Flash Express";
        deliveryNote.Status = DeliveryStatus.InTransit;
        deliveryNote.UpdatedAt = DateTime.UtcNow;
        deliveryNote.UpdatedBy = scannedBy;

        const int maxRetries = 3;
        int retryCount = 0;
        while (retryCount < maxRetries)
        {
            try
            {
                await _context.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    _logger.LogWarning(ex, "Concurrency conflict during barcode scan for delivery note {DeliveryNoteId}", deliveryNoteId);
                    throw new InvalidOperationException("The delivery note has been modified by another user. Please try again.", ex);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100 * retryCount), ct);
                
                // Refresh entity
                await _context.Entry(deliveryNote).ReloadAsync(ct);
                if (deliveryNote.Status != previousStatus)
                {
                    throw new InvalidOperationException("This delivery note has already been dispatched.");
                }
                deliveryNote.TrackingNumber = trimmedBarcode;
                deliveryNote.Status = DeliveryStatus.InTransit;
                deliveryNote.UpdatedAt = DateTime.UtcNow;
            }
        }

        _logger.LogInformation(
            "Barcode scanned for delivery note {DeliveryNoteId}: TrackingNumber={TrackingNumber}, ScannedBy={ScannedBy}",
            deliveryNoteId, trimmedBarcode, scannedBy);

        var cacheKey = $"delivery-note:{deliveryNoteId}";
        await _cache.RemoveAsync(cacheKey, ct);

        await PublishEventAsync(new DeliveryStatusChangedEvent
        {
            Payload = new DeliveryStatusChangedEventPayload
            {
                DeliveryNoteId = deliveryNote.DeliveryNoteId,
                OrderId = deliveryNote.OrderId,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = DeliveryStatus.InTransit.ToString(),
                ChangedAt = deliveryNote.UpdatedAt ?? DateTime.UtcNow,
                ChangedBy = scannedBy
            }
        }, ct);

        return new BarcodeScanResponse
        {
            DeliveryNoteId = deliveryNote.DeliveryNoteId,
            TrackingNumber = deliveryNote.TrackingNumber,
            CarrierName = deliveryNote.CarrierName!,
            Status = deliveryNote.Status.ToString()
        };
    }

    private async Task PublishEventAsync<T>(T eventMessage, CancellationToken ct) where T : class
    {
        var eventType = typeof(T).Name;
        var startTime = DateTime.UtcNow;

        try
        {
            await _publishEndpoint.Publish(eventMessage, context =>
            {
                // Set correlation ID for distributed tracing
                context.CorrelationId = Guid.NewGuid();

                // Add activity/trace ID for OpenTelemetry integration
                if (System.Diagnostics.Activity.Current != null)
                {
                    context.Headers.Set("TraceId", System.Diagnostics.Activity.Current.TraceId.ToString());
                    context.Headers.Set("SpanId", System.Diagnostics.Activity.Current.SpanId.ToString());
                }
            }, ct);

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug("Event published: {EventType}, Duration: {Duration}ms", eventType, duration);

            // Log structured metrics for OpenTelemetry
            _logger.LogInformation(
                "Event published successfully. EventType={EventType}, Duration={Duration}ms",
                eventType, duration);
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Graceful degradation: Log error but don't fail the operation
            _logger.LogWarning(ex,
                "Failed to publish event. EventType={EventType}, Duration={Duration}ms",
                eventType, duration);

            // Log metric for failed event publishing
            _logger.LogError(
                "Event publishing failed. EventType={EventType}, ErrorType={ErrorType}",
                eventType, ex.GetType().Name);
        }
    }
}
