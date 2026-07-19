using Maliev.DeliveryService.Application.DTOs;

namespace Maliev.DeliveryService.Application.Abstractions;

/// <summary>
/// Provides courier, rate, and tracking operations through the configured shipping gateway.
/// </summary>
public interface IShippingGatewayService
{
    /// <summary>
    /// Gets courier options supported by the configured shipping gateway.
    /// </summary>
    Task<IReadOnlyList<ShippingCourierResponse>> GetCouriersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets live shipping rate options for a shipment.
    /// </summary>
    Task<IReadOnlyList<ShippingRateOptionResponse>> GetRatesAsync(ShippingRateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets the latest tracking status for a shipment.
    /// </summary>
    Task<ShippingTrackingResponse> GetTrackingAsync(string trackingCode, CancellationToken ct = default);
}
