namespace Maliev.DeliveryService.Infrastructure.Shipping;

/// <summary>
/// Configuration for the GoShip Shipment Open API.
/// </summary>
public class GoShipOptions
{
    /// <summary>
    /// Gets or sets the configuration section name.
    /// </summary>
    public const string SectionName = "GoShip";

    /// <summary>
    /// Gets or sets the GoShip API base URL.
    /// </summary>
    public string BaseUrl { get; set; } = "https://uat-api.goship.express";

    /// <summary>
    /// Gets or sets the GoShip merchant app id used for request signing.
    /// </summary>
    public string? AppId { get; set; }

    /// <summary>
    /// Gets or sets the GoShip merchant secret used for request signing.
    /// </summary>
    public string? Secret { get; set; }
}
