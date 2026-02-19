namespace Maliev.DeliveryService.Data.Entities;

public class DeliveryNoteItem
{
    public long Id { get; set; }
    public string DeliveryNoteId { get; set; } = null!;
    public string? OrderId { get; set; }
    public long? PurchaseOrderItemId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityManufactured { get; set; }
    public decimal QuantityDelivered { get; set; }
    public string UnitOfMeasure { get; set; } = null!;
    public string? ItemNotes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation Property
    public DeliveryNote DeliveryNote { get; set; } = null!;
}
