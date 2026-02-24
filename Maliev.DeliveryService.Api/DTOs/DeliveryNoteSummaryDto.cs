namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>Initializes or represents a public member.</summary>
/// <summary>Initializes or represents a public member.</summary>
public class DeliveryNoteSummaryDto
{
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string DeliveryNoteId { get; set; } = null!;
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? OrderId { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? CustomerName { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public DateTime DeliveryDate { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string Status { get; set; } = null!;
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public int ItemCount { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? CarrierName { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? TrackingNumber { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public DateTime CreatedAt { get; set; }
}
