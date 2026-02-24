using Maliev.DeliveryService.Api.DTOs;

namespace Maliev.DeliveryService.Api.Services;

/// <summary>Initializes or represents a public member.</summary>
/// <summary>Initializes or represents a public member.</summary>
public interface IDeliveryNoteService
{
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    Task<DeliveryNoteResponse> CreateAsync(CreateDeliveryNoteRequest request, string createdBy, CancellationToken ct = default);
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    Task<DeliveryNoteResponse?> GetByIdAsync(string deliveryNoteId, CancellationToken ct = default);
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    Task<DeliveryNoteResponse> UpdateStatusAsync(string deliveryNoteId, UpdateDeliveryStatusRequest request, string updatedBy, CancellationToken ct = default);
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    Task<PaginatedResponse<DeliveryNoteSummaryDto>> SearchAsync(DeliveryNoteFilterRequest filter, string principalId, CancellationToken ct = default);
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    Task<DeliveryNoteFileResponse> AddFileAsync(string deliveryNoteId, IFormFile file, Data.Entities.FileType fileType, string? description, string uploadedBy, CancellationToken ct = default);
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    Task<List<DeliveryNoteFileResponse>> GetFilesAsync(string deliveryNoteId, CancellationToken ct = default);
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    Task<DeliveryNoteResponse> UpdateAsync(string deliveryNoteId, UpdateDeliveryNoteRequest request, string updatedBy, CancellationToken ct = default);
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    Task SoftDeleteAsync(string deliveryNoteId, string deletedBy, CancellationToken ct = default);
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    Task<BarcodeScanResponse> ScanBarcodeAsync(string deliveryNoteId, string barcodeValue, string scannedBy, CancellationToken ct = default);
}
