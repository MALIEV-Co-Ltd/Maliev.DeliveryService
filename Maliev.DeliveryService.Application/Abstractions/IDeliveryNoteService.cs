using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Domain.Entities;

namespace Maliev.DeliveryService.Application.Abstractions;

/// <summary>
/// Service for managing delivery notes.
/// </summary>
public interface IDeliveryNoteService
{
    /// <summary>
    /// Creates a new delivery note.
    /// </summary>
    Task<DeliveryNoteResponse> CreateAsync(CreateDeliveryNoteRequest request, string createdBy, CancellationToken ct = default);

    /// <summary>
    /// Gets a delivery note by its ID.
    /// </summary>
    Task<DeliveryNoteResponse?> GetByIdAsync(string deliveryNoteId, CancellationToken ct = default);

    /// <summary>
    /// Gets status audit entries for a delivery note.
    /// </summary>
    Task<List<DeliveryStatusAuditResponse>> GetStatusAuditsAsync(string deliveryNoteId, string principalId, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of an existing delivery note.
    /// </summary>
    Task<DeliveryNoteResponse> UpdateStatusAsync(string deliveryNoteId, UpdateDeliveryStatusRequest request, string updatedBy, CancellationToken ct = default);

    /// <summary>
    /// Searches for delivery notes based on filter criteria.
    /// </summary>
    Task<PaginatedResponse<DeliveryNoteSummaryDto>> SearchAsync(DeliveryNoteFilterRequest filter, string principalId, CancellationToken ct = default);

    /// <summary>
    /// Adds a file to a delivery note.
    /// </summary>
    Task<DeliveryNoteFileResponse> AddFileAsync(string deliveryNoteId, IFileData file, FileType fileType, string? description, string uploadedBy, CancellationToken ct = default);

    /// <summary>
    /// Gets all files attached to a delivery note.
    /// </summary>
    Task<List<DeliveryNoteFileResponse>> GetFilesAsync(string deliveryNoteId, CancellationToken ct = default);

    /// <summary>
    /// Downloads a file attached to a delivery note.
    /// </summary>
    Task<DeliveryNoteFileContentResponse> DownloadFileAsync(string deliveryNoteId, Guid fileId, string principalId, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing delivery note.
    /// </summary>
    Task<DeliveryNoteResponse> UpdateAsync(string deliveryNoteId, UpdateDeliveryNoteRequest request, string updatedBy, CancellationToken ct = default);

    /// <summary>
    /// Soft deletes a delivery note.
    /// </summary>
    Task SoftDeleteAsync(string deliveryNoteId, string deletedBy, CancellationToken ct = default);
}
