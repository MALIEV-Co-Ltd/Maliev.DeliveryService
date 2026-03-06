namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Response data for a delivery note item.
/// </summary>
public class DeliveryNoteItemResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the delivery note item.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the associated order ID.
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the associated purchase order item ID.
    /// </summary>
    public int? PurchaseOrderItemId { get; set; }

    /// <summary>
    /// Gets or sets the product code or SKU.
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Gets or sets the name of the product.
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    public string? ProductDescription { get; set; }

    /// <summary>
    /// Gets or sets the total quantity ordered.
    /// </summary>
    public decimal QuantityOrdered { get; set; }

    /// <summary>
    /// Gets or sets the total quantity manufactured.
    /// </summary>
    public decimal QuantityManufactured { get; set; }

    /// <summary>
    /// Gets or sets the quantity included in this delivery.
    /// </summary>
    public decimal QuantityDelivered { get; set; }

    /// <summary>
    /// Gets or sets the unit of measure.
    /// </summary>
    public string UnitOfMeasure { get; set; } = null!;

    /// <summary>
    /// Gets or sets optional notes for this item.
    /// </summary>
    public string? ItemNotes { get; set; }

    /// <summary>
    /// Gets or sets the concurrency version.
    /// </summary>
    public uint Version { get; set; }
}
