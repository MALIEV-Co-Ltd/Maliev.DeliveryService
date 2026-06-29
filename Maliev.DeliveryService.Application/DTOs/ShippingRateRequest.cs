using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Request for live shipping rate options.
/// </summary>
public class ShippingRateRequest
{
    /// <summary>
    /// Gets or sets the origin address.
    /// </summary>
    [Required]
    public ShippingAddressRequest From { get; set; } = new();

    /// <summary>
    /// Gets or sets the destination address.
    /// </summary>
    [Required]
    public ShippingAddressRequest To { get; set; } = new();

    /// <summary>
    /// Gets or sets the parcel dimensions and weight.
    /// </summary>
    [Required]
    public ShippingParcelRequest Parcel { get; set; } = new();

    /// <summary>
    /// Gets or sets optional SHIPPOP courier codes to restrict the quote.
    /// </summary>
    public List<string> CourierCodes { get; set; } = [];

    /// <summary>
    /// Gets or sets optional project parts to package from analyzed bounding boxes.
    /// When provided, DeliveryService calculates one or more packages from these parts.
    /// </summary>
    public List<ShippingPackagePartRequest> Parts { get; set; } = [];

    /// <summary>
    /// Gets or sets package planning constraints and packaging material margins.
    /// </summary>
    public ShippingPackagingOptionsRequest Packaging { get; set; } = new();

    /// <summary>
    /// Gets or sets whether standard public SHIPPOP rates should be used.
    /// </summary>
    public bool UsePublicRates { get; set; }
}
