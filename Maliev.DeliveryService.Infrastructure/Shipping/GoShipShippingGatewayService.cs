using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Application.Services;
using Microsoft.Extensions.Options;

namespace Maliev.DeliveryService.Infrastructure.Shipping;

/// <summary>
/// GoShip implementation of the shipping gateway service.
/// </summary>
public class GoShipShippingGatewayService : IGoShipShippingGatewayService
{
    private const string ProviderName = "GoShip";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly GoShipOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoShipShippingGatewayService"/> class.
    /// </summary>
    public GoShipShippingGatewayService(HttpClient httpClient, IOptions<GoShipOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingCourierResponse>> GetCouriersAsync(CancellationToken ct = default)
    {
        EnsureCredentialsConfigured();

        var path = BuildSignedPath("api/v3/carrier/list", new Dictionary<string, string> { ["limit"] = "100" });
        using var response = await _httpClient.GetAsync(path, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GoShip carrier list request failed with HTTP {(int)response.StatusCode}.");
        }

        var root = JsonNode.Parse(content) as JsonObject ?? throw new InvalidOperationException("GoShip carrier list response was empty.");
        var data = root["data"] as JsonArray ?? [];

        return data
            .OfType<JsonObject>()
            .Select(node => new ShippingCourierResponse
            {
                CourierCode = ReadString(node, "code") ?? string.Empty,
                CourierName = ReadString(node, "name") ?? string.Empty,
                Note = ReadString(node, "description"),
                LogoUrl = CourierLogoCatalog.ResolveLogoUrl(ReadString(node, "code"), ReadString(node, "name")),
                Provider = ProviderName
            })
            .Where(courier => !string.IsNullOrWhiteSpace(courier.CourierCode))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingRateOptionResponse>> GetRatesAsync(ShippingRateRequest request, CancellationToken ct = default)
    {
        EnsureCredentialsConfigured();

        var path = BuildSignedPath("api/v3/shipment/check-price", new Dictionary<string, string>());
        using var response = await _httpClient.PostAsJsonAsync(path, CreateCheckPricePayload(request), JsonOptions, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GoShip check-price request failed with HTTP {(int)response.StatusCode}.");
        }

        var root = JsonNode.Parse(content) as JsonObject ?? throw new InvalidOperationException("GoShip check-price response was empty.");
        var data = root["data"] as JsonArray ?? [];

        var rates = data
            .OfType<JsonObject>()
            .Select(ToRateOption)
            .Where(rate => !string.IsNullOrWhiteSpace(rate.CourierCode))
            .ToList();

        if (request.CourierCodes.Count > 0)
        {
            var allowed = new HashSet<string>(request.CourierCodes, StringComparer.OrdinalIgnoreCase);
            rates = rates.Where(rate => allowed.Contains(rate.CourierCode)).ToList();
        }

        if (rates.Count == 0)
        {
            throw new InvalidOperationException("GoShip did not return any shipping rate options.");
        }

        return rates;
    }

    /// <inheritdoc />
    public async Task<ShippingTrackingResponse> GetTrackingAsync(string trackingCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            throw new ArgumentException("Tracking code is required.", nameof(trackingCode));
        }

        EnsureCredentialsConfigured();

        var path = BuildSignedPath("api/v3/shipment", new Dictionary<string, string>
        {
            ["viewpoint"] = "all",
            ["search"] = trackingCode,
            ["limit"] = "1"
        });

        using var response = await _httpClient.GetAsync(path, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GoShip shipment search request failed with HTTP {(int)response.StatusCode}.");
        }

        var root = JsonNode.Parse(content) as JsonObject ?? throw new InvalidOperationException("GoShip shipment search response was empty.");
        var data = root["data"] as JsonArray ?? [];
        var summary = data.OfType<JsonObject>().FirstOrDefault()
            ?? throw new InvalidOperationException($"GoShip did not return shipment data for tracking code '{trackingCode}'.");

        // GoShip's shipment search response has no carrier display-name field and no event history,
        // unlike Shippop -- this is a permanent gap in the upstream API, not an oversight here.
        var courierCode = ReadString(summary, "carrier_code");

        return new ShippingTrackingResponse
        {
            TrackingCode = ReadString(summary, "tracking_number") ?? trackingCode,
            CourierCode = courierCode,
            CourierName = courierCode,
            Status = ReadString(summary, "status") ?? "unknown",
            Events = [],
            Provider = ProviderName
        };
    }

    private static object CreateCheckPricePayload(ShippingRateRequest request)
    {
        return new
        {
            box_width = request.Parcel.Width,
            box_height = request.Parcel.Height,
            box_length = request.Parcel.Length,
            box_weight = request.Parcel.Weight,
            origin = ToGoShipAddress(request.From),
            destination = ToGoShipAddress(request.To)
        };
    }

    private static object ToGoShipAddress(ShippingAddressRequest address)
    {
        return new
        {
            county = address.District,
            city = address.State,
            state = address.Province,
            postcode = address.Postcode
        };
    }

    private static ShippingRateOptionResponse ToRateOption(JsonObject node)
    {
        var courierCode = ReadString(node, "carrier_code") ?? string.Empty;
        return new ShippingRateOptionResponse
        {
            CourierCode = courierCode,
            CourierName = ReadString(node, "carrier") ?? courierCode,
            Price = ReadDecimal(node, "total", "price") ?? 0m,
            Currency = "THB",
            EstimatedDelivery = ReadString(node, "delivery_time"),
            CourierLogoUrl = CourierLogoCatalog.ResolveLogoUrl(courierCode, ReadString(node, "carrier")),
            Provider = ProviderName
        };
    }

    /// <summary>
    /// Builds the relative request path with the HMAC-SHA256 query signature GoShip requires
    /// (key = Base64("{timestamp}-{appId}"), signature = HMAC-SHA256("secret={secret}" + sorted "k=v" pairs)).
    /// </summary>
    private string BuildSignedPath(string relativePath, IDictionary<string, string> queryParams)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var key = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{timestamp}-{_options.AppId}"));

        var merged = new SortedDictionary<string, string>(queryParams, StringComparer.Ordinal)
        {
            ["key"] = key,
            ["timestamp"] = timestamp.ToString(CultureInfo.InvariantCulture)
        };

        var baseString = new StringBuilder("secret=" + _options.Secret);
        foreach (var pair in merged)
        {
            baseString.Append(pair.Key).Append('=').Append(pair.Value);
        }

        var signatureBytes = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_options.Secret ?? string.Empty),
            Encoding.UTF8.GetBytes(baseString.ToString()));

        // GoShip's reference implementations (Python hexdigest, Go hex.EncodeToString) emit lowercase hex;
        // Convert.ToHexString defaults to uppercase, which would silently fail signature verification.
        merged["signature"] = Convert.ToHexString(signatureBytes).ToLowerInvariant();

        var query = string.Join('&', merged.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return $"{relativePath}?{query}";
    }

    private void EnsureCredentialsConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.Secret))
        {
            throw new InvalidOperationException("GoShip app id and secret are not configured.");
        }
    }

    private static string? ReadString(JsonObject obj, params string[] names)
    {
        foreach (var name in names)
        {
            if (obj.TryGetPropertyValue(name, out var value) && value is not null)
            {
                return value.GetValueKind() == JsonValueKind.String
                    ? value.GetValue<string>()
                    : value.ToJsonString();
            }
        }

        return null;
    }

    private static decimal? ReadDecimal(JsonObject obj, params string[] names)
    {
        var raw = ReadString(obj, names);
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
