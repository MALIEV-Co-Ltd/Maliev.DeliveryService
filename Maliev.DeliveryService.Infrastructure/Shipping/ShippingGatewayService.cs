using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Maliev.DeliveryService.Infrastructure.Shipping;

/// <summary>
/// Orchestrates shipping gateway operations across SHIPPOP (primary) and GoShip (fallback).
/// </summary>
public sealed class ShippingGatewayService : IShippingGatewayService
{
    private readonly IShippopShippingGatewayService _shippop;
    private readonly IGoShipShippingGatewayService _goShip;
    private readonly ILogger<ShippingGatewayService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingGatewayService"/> class.
    /// </summary>
    public ShippingGatewayService(
        IShippopShippingGatewayService shippop,
        IGoShipShippingGatewayService goShip,
        ILogger<ShippingGatewayService> logger)
    {
        _shippop = shippop;
        _goShip = goShip;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingCourierResponse>> GetCouriersAsync(CancellationToken ct = default)
    {
        try
        {
            var couriers = (await _shippop.GetCouriersAsync(ct)).ToList();
            if (couriers.Count > 0)
            {
                return couriers;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shippop courier list request failed, falling back to GoShip");
        }

        _logger.LogDebug("Shippop returned no couriers, attempting GoShip fallback");
        return await _goShip.GetCouriersAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingRateOptionResponse>> GetRatesAsync(ShippingRateRequest request, CancellationToken ct = default)
    {
        try
        {
            var rates = (await _shippop.GetRatesAsync(request, ct)).ToList();
            if (rates.Count > 0)
            {
                return rates;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shippop rate request failed for courier(s) [{CourierCodes}], falling back to GoShip", string.Join(',', request.CourierCodes));
        }

        _logger.LogDebug("Shippop returned no rates, attempting GoShip fallback");
        return await _goShip.GetRatesAsync(request, ct);
    }

    /// <inheritdoc />
    public async Task<ShippingTrackingResponse> GetTrackingAsync(string trackingCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            throw new ArgumentException("Tracking code is required.", nameof(trackingCode));
        }

        try
        {
            var tracking = await _shippop.GetTrackingAsync(trackingCode, ct);
            if (tracking.Status != "unknown")
            {
                return tracking;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shippop tracking request failed for '{TrackingCode}', falling back to GoShip", trackingCode);
        }

        _logger.LogDebug("Shippop returned unknown tracking status for '{TrackingCode}', attempting GoShip fallback", trackingCode);
        return await _goShip.GetTrackingAsync(trackingCode, ct);
    }
}
