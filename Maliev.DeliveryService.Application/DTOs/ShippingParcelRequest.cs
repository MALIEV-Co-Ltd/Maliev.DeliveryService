using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Parcel dimensions and weight used for live shipping rate requests.
/// </summary>
public class ShippingParcelRequest
{
    /// <summary>
    /// Gets or sets the parcel display name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parcel weight in grams.
    /// </summary>
    [Range(1, double.MaxValue)]
    public decimal Weight { get; set; }

    /// <summary>
    /// Gets or sets the parcel width in centimeters.
    /// </summary>
    [Range(0.1, double.MaxValue)]
    public decimal Width { get; set; }

    /// <summary>
    /// Gets or sets the parcel length in centimeters.
    /// </summary>
    [Range(0.1, double.MaxValue)]
    public decimal Length { get; set; }

    /// <summary>
    /// Gets or sets the parcel height in centimeters.
    /// </summary>
    [Range(0.1, double.MaxValue)]
    public decimal Height { get; set; }
}
