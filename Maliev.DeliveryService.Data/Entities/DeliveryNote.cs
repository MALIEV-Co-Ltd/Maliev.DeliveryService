namespace Maliev.DeliveryService.Data.Entities;

/// <summary>Represents a delivery note (packing slip) for an order.</summary>
public class DeliveryNote
{
    /// <summary>Unique identifier for the delivery note.</summary>
    public string DeliveryNoteId { get; set; } = null!;
    /// <summary>Reference to the associated order ID.</summary>
    public string? OrderId { get; set; }
    /// <summary>Reference to the associated purchase order ID.</summary>
    public int? PurchaseOrderId { get; set; }
    /// <summary>Identifier of the customer receiving the delivery.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Name of the customer for display purposes.</summary>
    public string? CustomerName { get; set; }
    /// <summary>Scheduled or actual delivery date.</summary>
    public DateTime DeliveryDate { get; set; }
    /// <summary>The actual time when delivery was completed.</summary>
    public DateTime? ActualDeliveryTime { get; set; }
    /// <summary>Current status of the delivery.</summary>
    public DeliveryStatus Status { get; set; }

    /// <summary>Reference to the shipping address in the address book.</summary>
    public Guid? ShippingAddressId { get; set; }

    /// <summary>Snapshot of shipping address line 1 at time of creation.</summary>
    public string? ShippingAddressLine1 { get; set; }
    /// <summary>Snapshot of shipping address line 2 at time of creation.</summary>
    public string? ShippingAddressLine2 { get; set; }
    /// <summary>Snapshot of shipping city at time of creation.</summary>
    public string? ShippingCity { get; set; }
    /// <summary>Snapshot of shipping province at time of creation.</summary>
    public string? ShippingProvince { get; set; }
    /// <summary>Snapshot of shipping postal code at time of creation.</summary>
    public string? ShippingPostalCode { get; set; }
    /// <summary>Snapshot of shipping country at time of creation.</summary>
    public string? ShippingCountry { get; set; }

    /// <summary>Name of the contact person for this delivery.</summary>
    public string? DeliveryContactName { get; set; }
    /// <summary>Phone number of the contact person.</summary>
    public string? DeliveryContactPhone { get; set; }
    /// <summary>Email address of the contact person.</summary>
    public string? DeliveryContactEmail { get; set; }

    /// <summary>Name of the shipping carrier (e.g., Flash Express).</summary>
    public string? CarrierName { get; set; }
    /// <summary>Tracking number provided by the carrier.</summary>
    public string? TrackingNumber { get; set; }
    /// <summary>Cost of shipping charged for this delivery.</summary>
    public decimal? ShippingCost { get; set; }
    /// <summary>Currency code for the shipping cost (e.g., THB).</summary>
    public string? ShippingCostCurrency { get; set; }

    /// <summary>Name of the person who received the delivery.</summary>
    public string? ReceivedByName { get; set; }
    /// <summary>Identifier of the uploaded signature file image.</summary>
    public Guid? SignatureFileId { get; set; }
    /// <summary>Timestamp when the delivery was signed for.</summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>Internal operational notes about the delivery.</summary>
    public string? InternalNotes { get; set; }
    /// <summary>Specific instructions for the delivery driver.</summary>
    public string? DeliveryInstructions { get; set; }

    /// <summary>Timestamp when the record was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>User who created the record.</summary>
    public string CreatedBy { get; set; } = null!;
    /// <summary>Timestamp when the record was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>User who last updated the record.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Version number for optimistic concurrency control.</summary>
    public int RowVersion { get; set; }

    /// <summary>Indicates if the record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }
    /// <summary>Timestamp when the record was deleted.</summary>
    public DateTime? DeletedAt { get; set; }
    /// <summary>User who deleted the record.</summary>
    public string? DeletedBy { get; set; }

    /// <summary>Collection of items included in this delivery.</summary>
    public List<DeliveryNoteItem> Items { get; set; } = new();
    /// <summary>Collection of files (photos, signatures, documents) attached to this delivery.</summary>
    public List<DeliveryNoteFile> Files { get; set; } = new();
}
