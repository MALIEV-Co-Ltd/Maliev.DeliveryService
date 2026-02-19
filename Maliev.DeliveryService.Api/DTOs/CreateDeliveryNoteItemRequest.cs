using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Api.DTOs;

public class CreateDeliveryNoteItemRequest
{
    public string? OrderId { get; set; }
    public long? PurchaseOrderItemId { get; set; }

    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Quantity ordered must be >= 0")]
    public decimal QuantityOrdered { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Quantity manufactured must be >= 0")]
    public decimal QuantityManufactured { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity delivered must be > 0")]
    public decimal QuantityDelivered { get; set; }

    [Required]
    public string UnitOfMeasure { get; set; } = null!;

    public string? ItemNotes { get; set; }
}
