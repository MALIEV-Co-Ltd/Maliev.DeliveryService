namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>Response payload after a successful barcode scan.</summary>
public class BarcodeScanResponse
{
    /// <summary>The unique identifier of the updated delivery note.</summary>
    public string DeliveryNoteId { get; set; } = string.Empty;
    /// <summary>The tracking number parsed from the barcode.</summary>
    public string TrackingNumber { get; set; } = string.Empty;
    /// <summary>The carrier identified from the barcode format.</summary>
    public string CarrierName { get; set; } = string.Empty;
    /// <summary>The new status of the delivery note.</summary>
    public string Status { get; set; } = string.Empty;
}
