using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Package planning constraints and packaging material allowances.
/// </summary>
public class ShippingPackagingOptionsRequest
{
    /// <summary>
    /// Gets or sets the default per-side wrapping margin around each part in centimeters.
    /// </summary>
    [Range(0, 100)]
    public decimal PartMargin { get; set; } = 1.5m;

    /// <summary>
    /// Gets or sets the per-side box filler margin in centimeters.
    /// </summary>
    [Range(0, 100)]
    public decimal BoxMargin { get; set; } = 3m;

    /// <summary>
    /// Gets or sets the default per-part wrapping material weight in grams.
    /// </summary>
    [Range(0, 100_000)]
    public decimal PartPackagingWeight { get; set; } = 10m;

    /// <summary>
    /// Gets or sets the default outer-box filler and carton weight in grams.
    /// </summary>
    [Range(0, 100_000)]
    public decimal BoxPackagingWeight { get; set; } = 250m;

    /// <summary>
    /// Gets or sets the maximum preferred box weight in grams before splitting.
    /// </summary>
    [Range(1, double.MaxValue)]
    public decimal MaxPackageWeight { get; set; } = 20_000m;

    /// <summary>
    /// Gets or sets the maximum preferred package length in centimeters before splitting.
    /// </summary>
    [Range(1, double.MaxValue)]
    public decimal MaxPackageLength { get; set; } = 60m;

    /// <summary>
    /// Gets or sets the maximum preferred package width in centimeters before splitting.
    /// </summary>
    [Range(1, double.MaxValue)]
    public decimal MaxPackageWidth { get; set; } = 45m;

    /// <summary>
    /// Gets or sets the maximum preferred package height in centimeters before splitting.
    /// </summary>
    [Range(1, double.MaxValue)]
    public decimal MaxPackageHeight { get; set; } = 45m;
}
