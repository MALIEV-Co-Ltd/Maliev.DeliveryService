namespace Maliev.DeliveryService.Api.DTOs;

public class BarcodeScanResponse
{
    public string DeliveryNoteId { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
