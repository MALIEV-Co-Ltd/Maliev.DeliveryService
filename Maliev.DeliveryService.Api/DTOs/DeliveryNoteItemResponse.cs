namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>Initializes or represents a public member.</summary>
/// <summary>Initializes or represents a public member.</summary>
public class DeliveryNoteItemResponse
{
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public long Id { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? OrderId { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public long? PurchaseOrderItemId { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? ProductCode { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? ProductName { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? ProductDescription { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public decimal QuantityOrdered { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public decimal QuantityManufactured { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public decimal QuantityDelivered { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string UnitOfMeasure { get; set; } = null!;
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? ItemNotes { get; set; }
}
