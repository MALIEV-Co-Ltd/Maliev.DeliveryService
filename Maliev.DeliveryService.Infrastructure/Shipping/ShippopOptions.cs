namespace Maliev.DeliveryService.Infrastructure.Shipping;

/// <summary>
/// Configuration for SHIPPOP domestic and international APIs.
/// </summary>
public class ShippopOptions
{
    /// <summary>
    /// Gets or sets the configuration section name.
    /// </summary>
    public const string SectionName = "Shippop";

    /// <summary>
    /// Gets or sets the domestic SHIPPOP base URL.
    /// </summary>
    public string DomesticBaseUrl { get; set; } = "http://mkpservice.shippop.dev";

    /// <summary>
    /// Gets or sets the domestic SHIPPOP API key.
    /// </summary>
    public string? DomesticApiKey { get; set; }

    /// <summary>
    /// Gets or sets the account email used for booking APIs.
    /// </summary>
    public string? DomesticEmail { get; set; }

    /// <summary>
    /// Gets or sets the international SHIPPOP API base URL.
    /// </summary>
    public string? InternationalBaseUrl { get; set; } = "https://inter.shippop.dev";

    /// <summary>
    /// Gets or sets the international bearer token.
    /// </summary>
    public string? InternationalBearerToken { get; set; }
}
