namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Package breakdown used by a courier rate quote.
/// </summary>
public class ShippingPackageQuoteResponse
{
    /// <summary>
    /// Gets or sets the one-based package number.
    /// </summary>
    public int PackageNumber { get; set; }

    /// <summary>
    /// Gets or sets the package display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the package weight in grams.
    /// </summary>
    public decimal Weight { get; set; }

    /// <summary>
    /// Gets or sets the package width in centimeters.
    /// </summary>
    public decimal Width { get; set; }

    /// <summary>
    /// Gets or sets the package length in centimeters.
    /// </summary>
    public decimal Length { get; set; }

    /// <summary>
    /// Gets or sets the package height in centimeters.
    /// </summary>
    public decimal Height { get; set; }

    /// <summary>
    /// Gets or sets whether this package exceeds the preferred MALIEV box constraints.
    /// </summary>
    public bool IsOversized { get; set; }

    /// <summary>
    /// Gets or sets the courier price for this package.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the package quote currency.
    /// </summary>
    public string Currency { get; set; } = "THB";

    /// <summary>
    /// Gets or sets the courier lead-time text for this package.
    /// </summary>
    public string? EstimatedDelivery { get; set; }

    /// <summary>
    /// Gets or sets the packaged items inside this box.
    /// </summary>
    public List<ShippingPackageItemResponse> Items { get; set; } = [];
}

/// <summary>
/// Part quantity allocated into a planned shipping package.
/// </summary>
public class ShippingPackageItemResponse
{
    /// <summary>
    /// Gets or sets the part name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of this part in the package.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the wrapped unit width in centimeters.
    /// </summary>
    public decimal UnitWidth { get; set; }

    /// <summary>
    /// Gets or sets the wrapped unit length in centimeters.
    /// </summary>
    public decimal UnitLength { get; set; }

    /// <summary>
    /// Gets or sets the wrapped unit height in centimeters.
    /// </summary>
    public decimal UnitHeight { get; set; }

    /// <summary>
    /// Gets or sets the wrapped unit weight in grams.
    /// </summary>
    public decimal UnitWeight { get; set; }
}
