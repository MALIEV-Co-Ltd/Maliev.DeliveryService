using Maliev.DeliveryService.Application.DTOs;

namespace Maliev.DeliveryService.Application.Abstractions;

/// <summary>
/// Provides courier, rate, and tracking operations through the SHIPPOP gateway.
/// </summary>
public interface IShippopShippingGatewayService
{
    /// <summary>
    /// Gets courier options supported by SHIPPOP.
    /// </summary>
    Task<IReadOnlyList<ShippingCourierResponse>> GetCouriersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets live shipping rate options for a shipment from SHIPPOP.
    /// </summary>
    Task<IReadOnlyList<ShippingRateOptionResponse>> GetRatesAsync(ShippingRateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets the latest tracking status for a shipment from SHIPPOP.
    /// </summary>
    Task<ShippingTrackingResponse> GetTrackingAsync(string trackingCode, CancellationToken ct = default);
}
