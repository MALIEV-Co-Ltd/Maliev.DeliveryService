namespace Maliev.DeliveryService.Api.DTOs;

public class DeliveryNoteResponse
{
    public string DeliveryNoteId { get; set; } = null!;
    public string? OrderId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime DeliveryDate { get; set; }
    public DateTime? ActualDeliveryTime { get; set; }
    public string Status { get; set; } = null!;

    public string? ShippingAddressLine1 { get; set; }
    public string? ShippingAddressLine2 { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingProvince { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }

    public string? DeliveryContactName { get; set; }
    public string? DeliveryContactPhone { get; set; }
    public string? DeliveryContactEmail { get; set; }

    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal? ShippingCost { get; set; }
    public string? ShippingCostCurrency { get; set; }

    public string? ReceivedByName { get; set; }
    public DateTime? SignedAt { get; set; }

    public string? InternalNotes { get; set; }
    public string? DeliveryInstructions { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public int RowVersion { get; set; }

    public List<DeliveryNoteItemResponse> Items { get; set; } = new();
}
