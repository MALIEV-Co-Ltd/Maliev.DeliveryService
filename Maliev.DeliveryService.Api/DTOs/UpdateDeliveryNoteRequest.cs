namespace Maliev.DeliveryService.Api.DTOs;

public class UpdateDeliveryNoteRequest
{
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal? ShippingCost { get; set; }
    public string? ShippingCostCurrency { get; set; }

    public string? DeliveryContactName { get; set; }
    public string? DeliveryContactPhone { get; set; }
    public string? DeliveryContactEmail { get; set; }

    public string? DeliveryInstructions { get; set; }
    public string? InternalNotes { get; set; }

    public int RowVersion { get; set; } // For optimistic concurrency control
}
