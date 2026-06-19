namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Tracking event returned by a shipping gateway.
/// </summary>
public class ShippingTrackingEventResponse
{
    /// <summary>
    /// Gets or sets the event timestamp when available.
    /// </summary>
    public DateTimeOffset? OccurredAt { get; set; }

    /// <summary>
    /// Gets or sets the tracking status text.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event location.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the event description.
    /// </summary>
    public string? Description { get; set; }
}
