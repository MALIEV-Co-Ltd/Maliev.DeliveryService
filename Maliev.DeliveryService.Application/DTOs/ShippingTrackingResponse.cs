namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Current shipment tracking status.
/// </summary>
public class ShippingTrackingResponse
{
    /// <summary>
    /// Gets or sets the tracking code.
    /// </summary>
    public string TrackingCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the courier code when available.
    /// </summary>
    public string? CourierCode { get; set; }

    /// <summary>
    /// Gets or sets the courier display name when available.
    /// </summary>
    public string? CourierName { get; set; }

    /// <summary>
    /// Gets or sets the latest status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the latest status description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets tracking history events.
    /// </summary>
    public List<ShippingTrackingEventResponse> Events { get; set; } = [];
}
