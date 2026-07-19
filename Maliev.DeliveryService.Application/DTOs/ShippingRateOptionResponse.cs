namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Live shipping rate option returned by a shipping gateway.
/// </summary>
public class ShippingRateOptionResponse
{
    /// <summary>
    /// Gets or sets the courier code.
    /// </summary>
    public string CourierCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the courier display name.
    /// </summary>
    public string CourierName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quoted shipping price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the quote currency.
    /// </summary>
    public string Currency { get; set; } = "THB";

    /// <summary>
    /// Gets or sets the raw service level returned by the gateway.
    /// </summary>
    public string? ServiceLevel { get; set; }

    /// <summary>
    /// Gets or sets the estimated delivery or shipping lead-time description returned by the courier.
    /// </summary>
    public string? EstimatedDelivery { get; set; }

    /// <summary>
    /// Gets or sets the courier logo URL exposed to MALIEV clients.
    /// </summary>
    public string? CourierLogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the number of boxes included in this quoted rate.
    /// </summary>
    public int PackageCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the total quoted package weight in grams.
    /// </summary>
    public decimal TotalWeight { get; set; }

    /// <summary>
    /// Gets or sets the package breakdown used to request and aggregate this rate.
    /// </summary>
    public List<ShippingPackageQuoteResponse> Packages { get; set; } = [];

    /// <summary>
    /// Gets or sets the shipping gateway that served this rate option.
    /// </summary>
    public string Provider { get; set; } = string.Empty;
}
