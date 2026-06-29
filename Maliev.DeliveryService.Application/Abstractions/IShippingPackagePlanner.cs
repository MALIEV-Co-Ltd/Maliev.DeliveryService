using Maliev.DeliveryService.Application.DTOs;

namespace Maliev.DeliveryService.Application.Abstractions;

/// <summary>
/// Calculates one or more shipping packages from a manual parcel or project part bounding boxes.
/// </summary>
public interface IShippingPackagePlanner
{
    /// <summary>
    /// Creates the package breakdown used for live courier pricing.
    /// </summary>
    IReadOnlyList<ShippingPackageQuoteResponse> PlanPackages(ShippingRateRequest request);
}
