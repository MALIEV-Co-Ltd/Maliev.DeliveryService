using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Address payload used for shipping rate and booking requests.
/// </summary>
public class ShippingAddressRequest
{
    /// <summary>
    /// Gets or sets the contact name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the street address.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the district or subdistrict.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string District { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state or amphoe.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the province.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Province { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the postal code.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Postcode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact phone number.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Tel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact email.
    /// </summary>
    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the latitude for on-demand couriers.
    /// </summary>
    [MaxLength(50)]
    public string? Lat { get; set; }

    /// <summary>
    /// Gets or sets the longitude for on-demand couriers.
    /// </summary>
    [MaxLength(50)]
    public string? Lng { get; set; }
}
