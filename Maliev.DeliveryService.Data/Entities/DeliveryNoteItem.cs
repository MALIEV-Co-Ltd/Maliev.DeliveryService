namespace Maliev.DeliveryService.Data.Entities;

/// <summary>Represents an individual line item in a delivery note.</summary>
public class DeliveryNoteItem
{
    /// <summary>Unique identifier for the item record.</summary>
    public long Id { get; set; }
    /// <summary>Identifier of the associated delivery note.</summary>
    public string DeliveryNoteId { get; set; } = null!;
    /// <summary>Reference to the original order ID.</summary>
    public string? OrderId { get; set; }
    /// <summary>Reference to the original purchase order item ID.</summary>
    public long? PurchaseOrderItemId { get; set; }
    /// <summary>Code or SKU of the product.</summary>
    public string? ProductCode { get; set; }
    /// <summary>Name of the product.</summary>
    public string? ProductName { get; set; }
    /// <summary>Brief description of the product.</summary>
    public string? ProductDescription { get; set; }
    /// <summary>Quantity that was originally ordered.</summary>
    public decimal QuantityOrdered { get; set; }
    /// <summary>Quantity that has been manufactured.</summary>
    public decimal QuantityManufactured { get; set; }
    /// <summary>Quantity included in this delivery.</summary>
    public decimal QuantityDelivered { get; set; }
    /// <summary>Unit of measure (e.g., Pcs, Kg).</summary>
    public string UnitOfMeasure { get; set; } = null!;
    /// <summary>Notes specific to this line item.</summary>
    public string? ItemNotes { get; set; }
    /// <summary>Timestamp when the item was added to the delivery note.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>The delivery note this item belongs to.</summary>
    public DeliveryNote DeliveryNote { get; set; } = null!;
}
