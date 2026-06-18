namespace Maliev.DeliveryService.Domain.Entities;

/// <summary>
/// Durable audit record for a delivery note status transition.
/// </summary>
public class DeliveryStatusAudit
{
    /// <summary>
    /// Gets or sets the unique audit record identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the related delivery note identifier.
    /// </summary>
    public string DeliveryNoteId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the status before the transition.
    /// </summary>
    public DeliveryStatus PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the status after the transition.
    /// </summary>
    public DeliveryStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the user or system that changed the status.
    /// </summary>
    public string ChangedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets when the status changed.
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Gets or sets the related delivery note.
    /// </summary>
    public DeliveryNote DeliveryNote { get; set; } = null!;
}
