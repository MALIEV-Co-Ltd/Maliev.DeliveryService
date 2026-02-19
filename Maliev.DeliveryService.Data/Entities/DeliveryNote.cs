namespace Maliev.DeliveryService.Data.Entities;

public class DeliveryNote
{
    public string DeliveryNoteId { get; set; } = null!;
    public string? OrderId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime DeliveryDate { get; set; }
    public DateTime? ActualDeliveryTime { get; set; }
    public DeliveryStatus Status { get; set; }

    // Shipping Address Reference
    public Guid? ShippingAddressId { get; set; }

    // Denormalized Address Snapshot (immutable for audit trail)
    public string? ShippingAddressLine1 { get; set; }
    public string? ShippingAddressLine2 { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingProvince { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }

    // Delivery Contact
    public string? DeliveryContactName { get; set; }
    public string? DeliveryContactPhone { get; set; }
    public string? DeliveryContactEmail { get; set; }

    // Carrier Information
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal? ShippingCost { get; set; }
    public string? ShippingCostCurrency { get; set; }

    // Delivery Confirmation
    public string? ReceivedByName { get; set; }
    public Guid? SignatureFileId { get; set; }
    public DateTime? SignedAt { get; set; }

    // Notes
    public string? InternalNotes { get; set; }
    public string? DeliveryInstructions { get; set; }

    // Audit Fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Optimistic Concurrency
    public int RowVersion { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation Properties
    public List<DeliveryNoteItem> Items { get; set; } = new();
    public List<DeliveryNoteFile> Files { get; set; } = new();
}
