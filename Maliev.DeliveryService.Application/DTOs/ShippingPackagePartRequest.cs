using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// A project part and quantity used by DeliveryService to calculate package boxes.
/// Dimensions are bounding-box dimensions in centimeters.
/// </summary>
public class ShippingPackagePartRequest
{
    /// <summary>
    /// Gets or sets the part display name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordered quantity for this part.
    /// </summary>
    [Range(1, 100_000)]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Gets or sets the single-part weight in grams before wrapping.
    /// </summary>
    [Range(1, double.MaxValue)]
    public decimal Weight { get; set; }

    /// <summary>
    /// Gets or sets the part bounding-box width in centimeters.
    /// </summary>
    [Range(0.1, double.MaxValue)]
    public decimal Width { get; set; }

    /// <summary>
    /// Gets or sets the part bounding-box length in centimeters.
    /// </summary>
    [Range(0.1, double.MaxValue)]
    public decimal Length { get; set; }

    /// <summary>
    /// Gets or sets the part bounding-box height in centimeters.
    /// </summary>
    [Range(0.1, double.MaxValue)]
    public decimal Height { get; set; }

    /// <summary>
    /// Gets or sets an optional per-side wrapping margin in centimeters for this part.
    /// </summary>
    [Range(0, 100)]
    public decimal? PackagingMargin { get; set; }
}
