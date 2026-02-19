using Maliev.DeliveryService.Api.DTOs;

namespace Maliev.DeliveryService.Api.Services;

public interface IDeliveryNoteService
{
    Task<DeliveryNoteResponse> CreateAsync(CreateDeliveryNoteRequest request, string createdBy, CancellationToken ct = default);
    Task<DeliveryNoteResponse?> GetByIdAsync(string deliveryNoteId, CancellationToken ct = default);
    Task<DeliveryNoteResponse> UpdateStatusAsync(string deliveryNoteId, UpdateDeliveryStatusRequest request, string updatedBy, CancellationToken ct = default);
    Task<PaginatedResponse<DeliveryNoteSummaryDto>> SearchAsync(DeliveryNoteFilterRequest filter, string principalId, CancellationToken ct = default);
    Task<DeliveryNoteFileResponse> AddFileAsync(string deliveryNoteId, IFormFile file, Data.Entities.FileType fileType, string? description, string uploadedBy, CancellationToken ct = default);
    Task<List<DeliveryNoteFileResponse>> GetFilesAsync(string deliveryNoteId, CancellationToken ct = default);
    Task<DeliveryNoteResponse> UpdateAsync(string deliveryNoteId, UpdateDeliveryNoteRequest request, string updatedBy, CancellationToken ct = default);
    Task SoftDeleteAsync(string deliveryNoteId, string deletedBy, CancellationToken ct = default);
}
