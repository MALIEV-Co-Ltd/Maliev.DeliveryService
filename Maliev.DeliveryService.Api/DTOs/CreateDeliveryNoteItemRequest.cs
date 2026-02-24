using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>Request payload for an item within a new delivery note.</summary>
public class CreateDeliveryNoteItemRequest
{
    /// <summary>Associated order identifier.</summary>
    public string? OrderId { get; set; }
    /// <summary>Associated purchase order item identifier.</summary>
    public long? PurchaseOrderItemId { get; set; }

    /// <summary>Product code.</summary>
    public string? ProductCode { get; set; }
    /// <summary>Product name.</summary>
    public string? ProductName { get; set; }
    /// <summary>Product description.</summary>
    public string? ProductDescription { get; set; }

    /// <summary>Quantity that was ordered.</summary>
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Quantity ordered must be >= 0")]
    public decimal QuantityOrdered { get; set; }

    /// <summary>Quantity that has been manufactured.</summary>
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Quantity manufactured must be >= 0")]
    public decimal QuantityManufactured { get; set; }

    /// <summary>Quantity to be delivered.</summary>
    [Required]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity delivered must be > 0")]
    public decimal QuantityDelivered { get; set; }

    /// <summary>Unit of measure.</summary>
    [Required]
    public string UnitOfMeasure { get; set; } = null!;

    /// <summary>Notes for this specific item.</summary>
    public string? ItemNotes { get; set; }
}
