namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Courier option exposed to MALIEV clients.
/// </summary>
public class ShippingCourierResponse
{
    /// <summary>
    /// Gets or sets the courier code used by the shipping gateway.
    /// </summary>
    public string CourierCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the courier display name.
    /// </summary>
    public string CourierName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional courier notes.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Gets or sets the shipping scope, such as domestic or international.
    /// </summary>
    public string Scope { get; set; } = "domestic";
}
