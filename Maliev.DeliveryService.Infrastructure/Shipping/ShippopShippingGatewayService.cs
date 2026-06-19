using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;
using Microsoft.Extensions.Options;

namespace Maliev.DeliveryService.Infrastructure.Shipping;

/// <summary>
/// SHIPPOP implementation of the shipping gateway service.
/// </summary>
public class ShippopShippingGatewayService : IShippingGatewayService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly ShippopOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippopShippingGatewayService"/> class.
    /// </summary>
    public ShippopShippingGatewayService(HttpClient httpClient, IOptions<ShippopOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ShippingCourierResponse>> GetCouriersAsync(CancellationToken ct = default)
    {
        IReadOnlyList<ShippingCourierResponse> couriers =
        [
            new() { CourierCode = "SHF", CourierName = "SHIPPOP Fruit" },
            new() { CourierCode = "EMST", CourierName = "Thailand Post EMS" },
            new() { CourierCode = "ECP", CourierName = "Thailand Post eCo-post" },
            new() { CourierCode = "DHL", CourierName = "DHL" },
            new() { CourierCode = "FLE", CourierName = "Flash Express" },
            new() { CourierCode = "FLEB", CourierName = "Flash Express Bulky", Note = "Large parcel" },
            new() { CourierCode = "FLEF", CourierName = "Flash Express Fruit", Note = "Fruit and produce" },
            new() { CourierCode = "FLEDS", CourierName = "Flash Express Dropoff", Note = "Dropoff offline account required" },
            new() { CourierCode = "BEST", CourierName = "Best Express" },
            new() { CourierCode = "ARM", CourierName = "Aramex" },
            new() { CourierCode = "KRYX", CourierName = "KEX Exclusive" },
            new() { CourierCode = "KRYS", CourierName = "KEX Offline", Note = "Dropoff offline account required" },
            new() { CourierCode = "KRYDS", CourierName = "KEX Dropoff", Note = "Dropoff offline account required" },
            new() { CourierCode = "JNTP", CourierName = "J&T Express Pickup", Note = "Dropoff offline account required" },
            new() { CourierCode = "JNTD", CourierName = "J&T Express Dropoff", Note = "Dropoff offline account required" },
            new() { CourierCode = "LZDS", CourierName = "Lazada Dropoff", Note = "Dropoff offline account required" },
            new() { CourierCode = "MSE", CourierName = "Makesend" },
            new() { CourierCode = "MSEC", CourierName = "Makesend Chilled", Note = "Chilled goods" },
            new() { CourierCode = "MSEF", CourierName = "Makesend Frozen", Note = "Frozen goods" },
            new() { CourierCode = "SPX", CourierName = "SPX Express" },
            new() { CourierCode = "LLM", CourierName = "Lalamove", Note = "On-demand courier" },
            new() { CourierCode = "SKT", CourierName = "Skootar", Note = "On-demand courier" },
        ];

        return Task.FromResult(couriers);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingRateOptionResponse>> GetRatesAsync(ShippingRateRequest request, CancellationToken ct = default)
    {
        EnsureDomesticApiKeyConfigured();

        var path = request.UsePublicRates ? "public/pricelist/" : "pricelist/";
        using var response = await _httpClient.PostAsJsonAsync(path, CreateDomesticRatePayload(request), JsonOptions, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"SHIPPOP rate request failed with HTTP {(int)response.StatusCode}.");
        }

        var root = JsonNode.Parse(content) ?? throw new InvalidOperationException("SHIPPOP rate response was empty.");
        var rates = ExtractRateOptions(root).ToList();

        if (rates.Count == 0)
        {
            throw new InvalidOperationException("SHIPPOP did not return any shipping rate options.");
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

        using var response = await _httpClient.PostAsJsonAsync(
            "tracking/",
            new { tracking_code = trackingCode },
            JsonOptions,
            ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"SHIPPOP tracking request failed with HTTP {(int)response.StatusCode}.");
        }

        var root = JsonNode.Parse(content) ?? throw new InvalidOperationException("SHIPPOP tracking response was empty.");
        return ExtractTracking(root, trackingCode);
    }

    private object CreateDomesticRatePayload(ShippingRateRequest request)
    {
        return new
        {
            api_key = _options.DomesticApiKey,
            data = new Dictionary<string, object>
            {
                ["0"] = new
                {
                    from = ToShippopAddress(request.From),
                    to = ToShippopAddress(request.To),
                    parcel = new
                    {
                        name = request.Parcel.Name,
                        weight = request.Parcel.Weight,
                        width = request.Parcel.Width,
                        length = request.Parcel.Length,
                        height = request.Parcel.Height
                    },
                    courier_code = request.CourierCodes
                }
            }
        };
    }

    private static object ToShippopAddress(ShippingAddressRequest address)
    {
        return new
        {
            name = address.Name,
            address = address.Address,
            district = address.District,
            state = address.State,
            province = address.Province,
            postcode = address.Postcode,
            tel = address.Tel,
            email = address.Email,
            lat = address.Lat,
            lng = address.Lng
        };
    }

    private void EnsureDomesticApiKeyConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.DomesticApiKey))
        {
            throw new InvalidOperationException("SHIPPOP domestic API key is not configured.");
        }
    }

    private static IEnumerable<ShippingRateOptionResponse> ExtractRateOptions(JsonNode root)
    {
        foreach (var node in Descendants(root).OfType<JsonObject>())
        {
            var courierCode = ReadString(node, "courier_code", "courierCode", "code");
            var price = ReadDecimal(node, "price", "total_price", "totalPrice", "amount");

            if (string.IsNullOrWhiteSpace(courierCode) || price is null)
            {
                continue;
            }

            yield return new ShippingRateOptionResponse
            {
                CourierCode = courierCode,
                CourierName = ReadString(node, "courier_name", "courierName", "name") ?? courierCode,
                Price = price.Value,
                Currency = ReadString(node, "currency") ?? "THB",
                ServiceLevel = ReadString(node, "service_level", "serviceLevel", "service_type", "serviceType"),
                EstimatedDelivery = ReadString(node, "estimate_time", "estimatedDelivery", "delivery_time")
            };
        }
    }

    private static ShippingTrackingResponse ExtractTracking(JsonNode root, string trackingCode)
    {
        var objects = Descendants(root).OfType<JsonObject>().ToList();
        var summary = objects.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(ReadString(x, "tracking_code", "trackingCode", "tracking_number", "trackingNumber"))) ??
            objects.FirstOrDefault() ??
            new JsonObject();
        var events = objects
            .Where(x => !string.IsNullOrWhiteSpace(ReadString(x, "status", "description", "detail")))
            .Select(ToTrackingEvent)
            .Where(x => !string.IsNullOrWhiteSpace(x.Status) || !string.IsNullOrWhiteSpace(x.Description))
            .ToList();

        return new ShippingTrackingResponse
        {
            TrackingCode = ReadString(summary, "tracking_code", "trackingCode", "tracking_number", "trackingNumber") ?? trackingCode,
            CourierCode = ReadString(summary, "courier_code", "courierCode"),
            CourierName = ReadString(summary, "courier_name", "courierName"),
            Status = ReadString(summary, "status", "current_status", "currentStatus") ?? events.FirstOrDefault()?.Status ?? "unknown",
            Description = ReadString(summary, "description", "detail"),
            Events = events
        };
    }

    private static ShippingTrackingEventResponse ToTrackingEvent(JsonObject node)
    {
        return new ShippingTrackingEventResponse
        {
            OccurredAt = ReadDateTimeOffset(node, "date", "datetime", "created_at", "createdAt", "timestamp"),
            Status = ReadString(node, "status", "current_status", "currentStatus") ?? string.Empty,
            Location = ReadString(node, "location"),
            Description = ReadString(node, "description", "detail")
        };
    }

    private static IEnumerable<JsonNode> Descendants(JsonNode node)
    {
        yield return node;

        if (node is JsonObject obj)
        {
            foreach (var child in obj)
            {
                if (child.Value is not null)
                {
                    foreach (var descendant in Descendants(child.Value))
                    {
                        yield return descendant;
                    }
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array)
            {
                if (child is not null)
                {
                    foreach (var descendant in Descendants(child))
                    {
                        yield return descendant;
                    }
                }
            }
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

    private static DateTimeOffset? ReadDateTimeOffset(JsonObject obj, params string[] names)
    {
        var raw = ReadString(obj, names);
        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value)
            ? value
            : null;
    }
}
