using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Domain.Entities;
using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Delivery;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Maliev.DeliveryService.Infrastructure.Services;

/// <summary>
/// Implementation of delivery note service.
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
    /// Initializes a new instance of DeliveryNoteService.
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

    /// <inheritdoc />
    public async Task<DeliveryNoteResponse> CreateAsync(CreateDeliveryNoteRequest request, string createdBy, CancellationToken ct = default)
    {
        // Validate request
        ValidateCreateRequest(request);

        await EnsureCanAccessCustomerAsync(createdBy, request.CustomerId, ct);

        // Validate cumulative quantities for partial deliveries (if OrderId is provided)
        if (!string.IsNullOrEmpty(request.OrderId))
        {
            await ValidateCumulativeQuantitiesAsync(request, ct);
        }

        // Generate sequential ID
        var deliveryNoteId = await _idGenerator.GenerateNextIdAsync(ct);

        // Create entity
        var deliveryNote = request.ToEntity(deliveryNoteId, createdBy);

        _context.DeliveryNotes.Add(deliveryNote);

        await PublishEventAsync(new DeliveryNoteCreatedEvent(
            Guid.NewGuid(),
            nameof(DeliveryNoteCreatedEvent),
            MessageType.Event,
            "1.0",
            "DeliveryService",
            Array.Empty<string>(),
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow,
            false,
            new DeliveryNoteCreatedEventPayload(
                deliveryNote.DeliveryNoteId,
                deliveryNote.OrderId,
                deliveryNote.PurchaseOrderId,
                deliveryNote.CustomerId,
                deliveryNote.DeliveryDate,
                deliveryNote.Items.Count,
                deliveryNote.CreatedAt,
                deliveryNote.CreatedBy
            )), ct);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Delivery note created: {DeliveryNoteId}, OrderId: {OrderId}, CustomerId: {CustomerId}, CreatedBy: {CreatedBy}",
            deliveryNote.DeliveryNoteId, deliveryNote.OrderId, deliveryNote.CustomerId, createdBy);

        // Invalidate cache for this delivery note (in case it was previously cached and deleted)
        var cacheKey = $"delivery-note:{deliveryNote.DeliveryNoteId}";
        await _cache.RemoveAsync(cacheKey, ct);

        return deliveryNote.ToResponse();
    }

    /// <inheritdoc />
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
            .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == deliveryNoteId, ct);

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

    /// <inheritdoc />
    public async Task<PaginatedResponse<DeliveryNoteSummaryDto>> SearchAsync(
        DeliveryNoteFilterRequest filter,
        string principalId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Searching delivery notes: OrderId={OrderId}, CustomerId={CustomerId}, DateFrom={DateFrom}, DateTo={DateTo}, Status={Status}, Page={Page}, PageSize={PageSize}",
            filter.OrderId, filter.CustomerId, filter.DeliveryDateFrom, filter.DeliveryDateTo, filter.Status, filter.Page, filter.PageSize);

        var hasUnrestrictedAccess = await _authorizationService.HasUnrestrictedAccessAsync(principalId, ct);
        var authorizedCustomerIds = hasUnrestrictedAccess
            ? []
            : await _authorizationService.GetAuthorizedCustomerIdsAsync(principalId, ct);

        var query = _context.DeliveryNotes.AsQueryable();

        // Apply customer-scoped filter for non-admin/non-service callers. Empty authorization means no rows.
        if (!hasUnrestrictedAccess)
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
            if (!hasUnrestrictedAccess && !authorizedCustomerIds.Contains(filter.CustomerId.Value))
            {
                return new PaginatedResponse<DeliveryNoteSummaryDto>
                {
                    Items = [],
                    TotalCount = 0,
                    Page = filter.Page,
                    PageSize = Math.Min(filter.PageSize, 100)
                };
            }

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

    /// <inheritdoc />
    public async Task<DeliveryNoteResponse> UpdateStatusAsync(
        string deliveryNoteId,
        UpdateDeliveryStatusRequest request,
        string updatedBy,
        CancellationToken ct = default)
    {
        var deliveryNote = await _context.DeliveryNotes
            .Include(dn => dn.Items)
            .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == deliveryNoteId, ct);

        if (deliveryNote == null)
        {
            throw new InvalidOperationException($"Delivery note {deliveryNoteId} not found");
        }

        await EnsureCanAccessCustomerAsync(updatedBy, deliveryNote.CustomerId, ct);

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

        await PublishEventAsync(new DeliveryStatusChangedEvent(
            Guid.NewGuid(),
            nameof(DeliveryStatusChangedEvent),
            MessageType.Event,
            "1.0",
            "DeliveryService",
            ["NotificationService"],
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow,
            false,
            new DeliveryStatusChangedEventPayload(
                deliveryNote.DeliveryNoteId,
                deliveryNote.OrderId,
                deliveryNote.CustomerId,
                oldStatus.ToString(),
                newStatus.ToString(),
                deliveryNote.ActualDeliveryTime,
                deliveryNote.ReceivedByName,
                deliveryNote.UpdatedAt ?? DateTimeOffset.UtcNow,
                updatedBy
            )), ct);

        if (newStatus == DeliveryStatus.Delivered)
        {
            await PublishEventAsync(new DeliveryCompletedEvent(
                Guid.NewGuid(),
                nameof(DeliveryCompletedEvent),
                MessageType.Event,
                "1.0",
                "DeliveryService",
                ["NotificationService"],
                Guid.NewGuid(),
                null,
                DateTimeOffset.UtcNow,
                false,
                new DeliveryCompletedEventPayload(
                    deliveryNote.DeliveryNoteId,
                    deliveryNote.OrderId,
                    deliveryNote.PurchaseOrderId,
                    deliveryNote.CustomerId,
                    deliveryNote.ActualDeliveryTime ?? DateTimeOffset.UtcNow,
                    deliveryNote.ReceivedByName ?? string.Empty
                )), ct);
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Delivery note status updated: {DeliveryNoteId}, Status: {OldStatus} -> {NewStatus}, UpdatedBy: {UpdatedBy}",
            deliveryNoteId, oldStatus, newStatus, updatedBy);

        // Invalidate cache for this delivery note
        var cacheKey = $"delivery-note:{deliveryNoteId}";
        await _cache.RemoveAsync(cacheKey, ct);

        return deliveryNote.ToResponse();
    }

    /// <inheritdoc />
    public async Task<DeliveryNoteFileResponse> AddFileAsync(
        string deliveryNoteId,
        IFileData file,
        FileType fileType,
        string? description,
        string uploadedBy,
        CancellationToken ct = default)
    {
        // Validate delivery note exists
        var deliveryNote = await _context.DeliveryNotes
            .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == deliveryNoteId, ct);

        if (deliveryNote == null)
        {
            throw new InvalidOperationException($"Delivery note {deliveryNoteId} not found");
        }

        await EnsureCanAccessCustomerAsync(uploadedBy, deliveryNote.CustomerId, ct);

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
            var deliveryNoteFile = new DeliveryNoteFile
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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
                    .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == deliveryNoteId, ct);

                if (deliveryNote == null)
                {
                    throw new InvalidOperationException($"Delivery note {deliveryNoteId} not found");
                }

                await EnsureCanAccessCustomerAsync(updatedBy, deliveryNote.CustomerId, ct);

                // Cannot update terminal states
                if (deliveryNote.Status == DeliveryStatus.Delivered ||
                    deliveryNote.Status == DeliveryStatus.Cancelled)
                {
                    throw new InvalidOperationException($"Cannot update delivery note in terminal status {deliveryNote.Status}");
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

    /// <inheritdoc />
    public async Task SoftDeleteAsync(string deliveryNoteId, string deletedBy, CancellationToken ct = default)
    {
        var deliveryNote = await _context.DeliveryNotes
            .FirstOrDefaultAsync(dn => dn.DeliveryNoteId == deliveryNoteId, ct);

        if (deliveryNote == null)
        {
            throw new InvalidOperationException($"Delivery note {deliveryNoteId} not found");
        }

        await EnsureCanAccessCustomerAsync(deletedBy, deliveryNote.CustomerId, ct);

        // Only allow deletion of Pending status
        if (deliveryNote.Status != DeliveryStatus.Pending)
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

    private async Task EnsureCanAccessCustomerAsync(string principalId, Guid customerId, CancellationToken ct)
    {
        if (await _authorizationService.HasUnrestrictedAccessAsync(principalId, ct))
        {
            return;
        }

        if (await _authorizationService.CanAccessCustomerAsync(principalId, customerId, ct))
        {
            return;
        }

        throw new UnauthorizedAccessException($"Principal {principalId} cannot access customer {customerId}");
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

            _logger.LogWarning(ex,
                "Failed to publish event. EventType={EventType}, Duration={Duration}ms",
                eventType, duration);

            // Log metric for failed event publishing
            _logger.LogError(
                "Event publishing failed. EventType={EventType}, ErrorType={ErrorType}",
                eventType, ex.GetType().Name);

            throw;
        }
    }
}
