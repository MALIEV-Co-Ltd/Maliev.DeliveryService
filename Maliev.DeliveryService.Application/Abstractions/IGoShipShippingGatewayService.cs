using Maliev.DeliveryService.Application.DTOs;

namespace Maliev.DeliveryService.Application.Abstractions;

/// <summary>
/// Provides courier, rate, and tracking operations through the GoShip gateway.
/// </summary>
public interface IGoShipShippingGatewayService
{
    /// <summary>
    /// Gets courier options supported by GoShip.
    /// </summary>
    Task<IReadOnlyList<ShippingCourierResponse>> GetCouriersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets live shipping rate options for a shipment from GoShip.
    /// </summary>
    Task<IReadOnlyList<ShippingRateOptionResponse>> GetRatesAsync(ShippingRateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets the latest tracking status for a shipment from GoShip.
    /// </summary>
    Task<ShippingTrackingResponse> GetTrackingAsync(string trackingCode, CancellationToken ct = default);
}
