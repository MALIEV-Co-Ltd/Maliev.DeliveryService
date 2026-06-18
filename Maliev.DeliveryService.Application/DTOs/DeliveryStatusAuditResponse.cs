namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Response data for a delivery note status audit entry.
/// </summary>
public class DeliveryStatusAuditResponse
{
    /// <summary>
    /// Gets or sets the audit entry identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the related delivery note identifier.
    /// </summary>
    public string DeliveryNoteId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the status before the transition.
    /// </summary>
    public string PreviousStatus { get; set; } = null!;

    /// <summary>
    /// Gets or sets the status after the transition.
    /// </summary>
    public string NewStatus { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user or system that changed the status.
    /// </summary>
    public string ChangedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets when the status changed.
    /// </summary>
    public DateTime ChangedAt { get; set; }
}
