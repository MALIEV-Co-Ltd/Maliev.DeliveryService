using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Application.Services;
using Microsoft.Extensions.Logging;

namespace Maliev.DeliveryService.Infrastructure.Shipping;

/// <summary>
/// Orchestrates shipping gateway operations across SHIPPOP (primary) and GoShip (fallback).
/// </summary>
public sealed class ShippingGatewayService : IShippingGatewayService
{
    private readonly IShippopShippingGatewayService _shippop;
    private readonly IGoShipShippingGatewayService _goShip;
    private readonly IShippingPackagePlanner _packagePlanner;
    private readonly ILogger<ShippingGatewayService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingGatewayService"/> class.
    /// </summary>
    public ShippingGatewayService(
        IShippopShippingGatewayService shippop,
        IGoShipShippingGatewayService goShip,
        IShippingPackagePlanner packagePlanner,
        ILogger<ShippingGatewayService> logger)
    {
        _shippop = shippop;
        _goShip = goShip;
        _packagePlanner = packagePlanner;
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
                return DecorateCouriers(couriers);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shippop courier list request failed, falling back to GoShip");
        }

        _logger.LogDebug("Shippop returned no couriers, attempting GoShip fallback");
        return DecorateCouriers(await _goShip.GetCouriersAsync(ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingRateOptionResponse>> GetRatesAsync(ShippingRateRequest request, CancellationToken ct = default)
    {
        var packages = _packagePlanner.PlanPackages(request);
        var packageRateSets = new List<PackageRateSet>();

        foreach (var package in packages)
        {
            var packageRequest = CreatePackageRateRequest(request, package);
            var rates = (await GetGatewayRatesForSinglePackageAsync(packageRequest, ct))
                .Select(DecorateRate)
                .GroupBy(RateAggregationKey.Create)
                .Select(group => group.OrderBy(rate => rate.Price).First())
                .ToList();

            packageRateSets.Add(new PackageRateSet(package, rates));
        }

        return AggregatePackageRates(packageRateSets);
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

    private async Task<IReadOnlyList<ShippingRateOptionResponse>> GetGatewayRatesForSinglePackageAsync(
        ShippingRateRequest request,
        CancellationToken ct)
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

    private static IReadOnlyList<ShippingCourierResponse> DecorateCouriers(IEnumerable<ShippingCourierResponse> couriers)
    {
        return couriers
            .Select(courier =>
            {
                courier.LogoUrl ??= CourierLogoCatalog.ResolveLogoUrl(courier.CourierCode, courier.CourierName);
                return courier;
            })
            .ToList();
    }

    private static ShippingRateOptionResponse DecorateRate(ShippingRateOptionResponse rate)
    {
        rate.CourierLogoUrl ??= CourierLogoCatalog.ResolveLogoUrl(rate.CourierCode, rate.CourierName);
        return rate;
    }

    private static ShippingRateRequest CreatePackageRateRequest(ShippingRateRequest source, ShippingPackageQuoteResponse package)
    {
        return new ShippingRateRequest
        {
            From = source.From,
            To = source.To,
            Parcel = new ShippingParcelRequest
            {
                Name = package.Name,
                Weight = package.Weight,
                Width = package.Width,
                Length = package.Length,
                Height = package.Height
            },
            CourierCodes = source.CourierCodes,
            UsePublicRates = source.UsePublicRates
        };
    }

    private static IReadOnlyList<ShippingRateOptionResponse> AggregatePackageRates(IReadOnlyList<PackageRateSet> packageRateSets)
    {
        if (packageRateSets.Count == 0)
        {
            return [];
        }

        var requiredPackageCount = packageRateSets.Count;
        var rates = packageRateSets
            .SelectMany(set => set.Rates.Select(rate => new PackageRate(set.Package, rate)))
            .GroupBy(item => RateAggregationKey.Create(item.Rate))
            .Where(group => group.Select(item => item.Package.PackageNumber).Distinct().Count() == requiredPackageCount)
            .Select(group => ToAggregateRate(group.OrderBy(item => item.Package.PackageNumber).ToList()))
            .OrderBy(rate => rate.Price)
            .ToList();

        if (rates.Count == 0)
        {
            throw new InvalidOperationException("No courier could quote every planned shipping package.");
        }

        return rates;
    }

    private static ShippingRateOptionResponse ToAggregateRate(IReadOnlyList<PackageRate> packageRates)
    {
        var first = packageRates[0].Rate;
        var currencies = packageRates
            .Select(item => string.IsNullOrWhiteSpace(item.Rate.Currency) ? "THB" : item.Rate.Currency)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var packages = packageRates
            .Select(item => new ShippingPackageQuoteResponse
            {
                PackageNumber = item.Package.PackageNumber,
                Name = item.Package.Name,
                Weight = item.Package.Weight,
                Width = item.Package.Width,
                Length = item.Package.Length,
                Height = item.Package.Height,
                IsOversized = item.Package.IsOversized,
                Price = item.Rate.Price,
                Currency = string.IsNullOrWhiteSpace(item.Rate.Currency) ? "THB" : item.Rate.Currency,
                EstimatedDelivery = item.Rate.EstimatedDelivery,
                Items = item.Package.Items
            })
            .ToList();

        return new ShippingRateOptionResponse
        {
            CourierCode = first.CourierCode,
            CourierName = first.CourierName,
            Price = packages.Sum(package => package.Price),
            Currency = currencies.Count == 1 ? currencies[0] : "MIXED",
            ServiceLevel = first.ServiceLevel,
            EstimatedDelivery = CombineEstimatedDelivery(packageRates.Select(item => item.Rate.EstimatedDelivery)),
            CourierLogoUrl = first.CourierLogoUrl ?? CourierLogoCatalog.ResolveLogoUrl(first.CourierCode, first.CourierName),
            PackageCount = packages.Count,
            TotalWeight = packages.Sum(package => package.Weight),
            Packages = packages,
            Provider = first.Provider
        };
    }

    private static string? CombineEstimatedDelivery(IEnumerable<string?> estimatedDeliveries)
    {
        var values = estimatedDeliveries
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values.Count switch
        {
            0 => null,
            1 => values[0],
            _ => "Varies by package: " + string.Join(", ", values)
        };
    }

    private sealed record PackageRateSet(ShippingPackageQuoteResponse Package, IReadOnlyList<ShippingRateOptionResponse> Rates);

    private sealed record PackageRate(ShippingPackageQuoteResponse Package, ShippingRateOptionResponse Rate);

    private sealed record RateAggregationKey(string Provider, string CourierCode, string? ServiceLevel)
    {
        public static RateAggregationKey Create(ShippingRateOptionResponse rate) =>
            new(
                rate.Provider.Trim(),
                rate.CourierCode.Trim(),
                string.IsNullOrWhiteSpace(rate.ServiceLevel) ? null : rate.ServiceLevel.Trim());
    }
}
