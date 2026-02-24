namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>Initializes or represents a public member.</summary>
/// <summary>Initializes or represents a public member.</summary>
public class UpdateDeliveryNoteRequest
{
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? CarrierName { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? TrackingNumber { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public decimal? ShippingCost { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? ShippingCostCurrency { get; set; }

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? DeliveryContactName { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? DeliveryContactPhone { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? DeliveryContactEmail { get; set; }

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? DeliveryInstructions { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? InternalNotes { get; set; }

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public int RowVersion { get; set; } // For optimistic concurrency control
}
