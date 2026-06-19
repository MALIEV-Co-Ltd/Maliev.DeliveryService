using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Infrastructure.Shipping;
using Microsoft.Extensions.Options;

namespace Maliev.DeliveryService.Tests.Unit.Clients;

public class GoShipShippingGatewayServiceTests
{
    private const string AppId = "test-app";
    private const string Secret = "test-secret";

    [Fact]
    public async Task GetCouriers_ParsesCarrierListAndSignsRequest()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.OK,
            """
            {
              "data": [
                { "name": "Kerry Express", "description": "Kerry Express Thailand", "code": "kerry" },
                { "name": "Flash Express", "description": "Flash Express Thailand", "code": "flash" }
              ]
            }
            """);
        var service = CreateService(handler);

        var couriers = await service.GetCouriersAsync(CancellationToken.None);

        Assert.NotNull(handler.RequestUri);
        Assert.Equal("/api/v3/carrier/list", handler.RequestUri!.AbsolutePath);
        AssertValidSignature(handler.RequestUri);

        Assert.Equal(2, couriers.Count);
        Assert.Contains(couriers, x => x.CourierCode == "kerry" && x.CourierName == "Kerry Express" && x.Provider == "GoShip");
        Assert.Contains(couriers, x => x.CourierCode == "flash" && x.CourierName == "Flash Express");
    }

    [Fact]
    public async Task GetRates_SendsCheckPricePayloadAndParsesRates()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.OK,
            """
            {
              "data": [
                { "carrier": "Kerry Express", "carrier_code": "kerry", "price": "300.0000", "total": "359.5000", "delivery_time": "1-2 days" },
                { "carrier": "Flash Express", "carrier_code": "flash", "price": "200.0000", "total": "240.0000", "delivery_time": "2-3 days" }
              ]
            }
            """);
        var service = CreateService(handler);

        var rates = await service.GetRatesAsync(CreateRateRequest(courierCodes: ["kerry"]), CancellationToken.None);

        Assert.Equal("/api/v3/shipment/check-price", handler.RequestUri!.AbsolutePath);
        AssertValidSignature(handler.RequestUri);

        using var document = JsonDocument.Parse(handler.Body);
        Assert.Equal(500, document.RootElement.GetProperty("box_weight").GetInt32());
        Assert.Equal(10, document.RootElement.GetProperty("box_width").GetInt32());
        Assert.Equal("Pathum Wan", document.RootElement.GetProperty("origin").GetProperty("county").GetString());
        Assert.Equal("Silom", document.RootElement.GetProperty("destination").GetProperty("county").GetString());

        var rate = Assert.Single(rates);
        Assert.Equal("kerry", rate.CourierCode);
        Assert.Equal("Kerry Express", rate.CourierName);
        Assert.Equal(359.5m, rate.Price);
        Assert.Equal("GoShip", rate.Provider);
    }

    [Fact]
    public async Task GetRates_WhenCredentialsMissing_FailsClosedBeforeHttpCall()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler, appId: "");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetRatesAsync(CreateRateRequest(), CancellationToken.None));
        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task GetTracking_SendsShipmentSearchQueryAndParsesResponse()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.OK,
            """
            {
              "data": [
                { "tracking_number": "GS123456", "carrier_code": "kerry", "status": "in_transit" }
              ]
            }
            """);
        var service = CreateService(handler);

        var tracking = await service.GetTrackingAsync("GS123456", CancellationToken.None);

        Assert.Equal("/api/v3/shipment", handler.RequestUri!.AbsolutePath);
        var query = ParseQuery(handler.RequestUri);
        Assert.Equal("all", query["viewpoint"]);
        Assert.Equal("GS123456", query["search"]);
        Assert.Equal("1", query["limit"]);
        AssertValidSignature(handler.RequestUri);

        Assert.Equal("GS123456", tracking.TrackingCode);
        Assert.Equal("kerry", tracking.CourierCode);
        Assert.Equal("kerry", tracking.CourierName);
        Assert.Equal("in_transit", tracking.Status);
        Assert.Empty(tracking.Events);
        Assert.Equal("GoShip", tracking.Provider);
    }

    private static GoShipShippingGatewayService CreateService(RecordingHandler handler, string appId = AppId, string secret = Secret)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.goship.test/")
        };
        var options = Options.Create(new GoShipOptions
        {
            BaseUrl = "https://api.goship.test",
            AppId = appId,
            Secret = secret
        });

        return new GoShipShippingGatewayService(httpClient, options);
    }

    private static ShippingRateRequest CreateRateRequest(IEnumerable<string>? courierCodes = null)
    {
        return new ShippingRateRequest
        {
            From = new ShippingAddressRequest
            {
                Name = "MALIEV",
                Address = "1",
                District = "Pathum Wan",
                State = "Pathum Wan",
                Province = "Bangkok",
                Postcode = "10400",
                Tel = "020000000"
            },
            To = new ShippingAddressRequest
            {
                Name = "Customer",
                Address = "2",
                District = "Silom",
                State = "Bang Rak",
                Province = "Bangkok",
                Postcode = "10500",
                Tel = "0800000000"
            },
            Parcel = new ShippingParcelRequest
            {
                Name = "Printed part",
                Weight = 500,
                Width = 10,
                Length = 20,
                Height = 5
            },
            CourierCodes = courierCodes?.ToList() ?? []
        };
    }

    private static Dictionary<string, string> ParseQuery(Uri uri)
    {
        return uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(parts => Uri.UnescapeDataString(parts[0]), parts => Uri.UnescapeDataString(parts[1]));
    }

    private static void AssertValidSignature(Uri requestUri)
    {
        var query = ParseQuery(requestUri);
        var signature = query["signature"];
        var timestamp = query["timestamp"];
        var key = query["key"];

        Assert.Equal($"{timestamp}-{AppId}", Encoding.UTF8.GetString(Convert.FromBase64String(key)));

        var signedParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in query)
        {
            if (pair.Key != "signature")
            {
                signedParams[pair.Key] = pair.Value;
            }
        }

        var baseString = new StringBuilder("secret=" + Secret);
        foreach (var pair in signedParams)
        {
            baseString.Append(pair.Key).Append('=').Append(pair.Value);
        }

        var expectedSignature = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(baseString.ToString()))).ToLowerInvariant();

        Assert.Equal(expectedSignature, signature);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _response;

        public RecordingHandler(HttpStatusCode statusCode, string response)
        {
            _statusCode = statusCode;
            _response = response;
        }

        public Uri? RequestUri { get; private set; }

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json")
            };
        }
    }
}
