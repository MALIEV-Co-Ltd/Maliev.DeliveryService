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
    /// Gets or sets the estimated delivery description.
    /// </summary>
    public string? EstimatedDelivery { get; set; }

    /// <summary>
    /// Gets or sets the shipping gateway that served this rate option.
    /// </summary>
    public string Provider { get; set; } = string.Empty;
}
