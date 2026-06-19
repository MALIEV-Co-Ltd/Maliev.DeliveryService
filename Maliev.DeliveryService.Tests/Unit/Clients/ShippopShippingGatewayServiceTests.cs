using System.Net;
using System.Text;
using System.Text.Json;

using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Infrastructure.Shipping;
using Microsoft.Extensions.Options;

namespace Maliev.DeliveryService.Tests.Unit.Clients;

public class ShippopShippingGatewayServiceTests
{
    [Fact]
    public async Task GetCouriers_ReturnsDocumentedDomesticCouriers()
    {
        var service = CreateService(new RecordingHandler(HttpStatusCode.OK, "{}"));

        var couriers = await service.GetCouriersAsync(CancellationToken.None);

        Assert.Contains(couriers, x => x.CourierCode == "EMST");
        Assert.Contains(couriers, x => x.CourierCode == "DHL");
        Assert.Contains(couriers, x => x.CourierCode == "LLM");
    }

    [Fact]
    public async Task GetRates_SendsShippopPricelistPayloadAndParsesRates()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.OK,
            """
            {
              "status": true,
              "data": {
                "0": {
                  "EMST": {
                    "courier_code": "EMST",
                    "courier_name": "Thailand Post EMS",
                    "price": 37
                  }
                }
              }
            }
            """);
        var service = CreateService(handler);

        var rates = await service.GetRatesAsync(CreateRateRequest(), CancellationToken.None);

        Assert.Equal("https://mkpservice.shippop.test/pricelist/", handler.RequestUri?.ToString());
        using var document = JsonDocument.Parse(handler.Body);
        Assert.Equal("test-key", document.RootElement.GetProperty("api_key").GetString());
        Assert.True(document.RootElement.GetProperty("data").TryGetProperty("0", out var shipment));
        Assert.Equal("10400", shipment.GetProperty("from").GetProperty("postcode").GetString());
        Assert.Equal("EMST", shipment.GetProperty("courier_code")[0].GetString());
        var rate = Assert.Single(rates);
        Assert.Equal("EMST", rate.CourierCode);
        Assert.Equal(37m, rate.Price);
    }

    [Fact]
    public async Task GetRates_WhenApiKeyMissing_FailsClosedBeforeHttpCall()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler, domesticApiKey: "");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetRatesAsync(CreateRateRequest(), CancellationToken.None));
        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task GetTracking_SendsTrackingPayloadAndParsesStatus()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.OK,
            """
            {
              "tracking_code": "SP529189074",
              "courier_code": "EMST",
              "courier_name": "Thailand Post EMS",
              "status": "delivered",
              "description": "Delivered to recipient",
              "history": [
                {
                  "date": "2026-06-19T10:00:00Z",
                  "status": "delivered",
                  "description": "Delivered to recipient"
                }
              ]
            }
            """);
        var service = CreateService(handler);

        var tracking = await service.GetTrackingAsync("SP529189074", CancellationToken.None);

        Assert.Equal("https://mkpservice.shippop.test/tracking/", handler.RequestUri?.ToString());
        using var document = JsonDocument.Parse(handler.Body);
        Assert.Equal("SP529189074", document.RootElement.GetProperty("tracking_code").GetString());
        Assert.Equal("SP529189074", tracking.TrackingCode);
        Assert.Equal("delivered", tracking.Status);
        Assert.NotEmpty(tracking.Events);
    }

    private static ShippopShippingGatewayService CreateService(RecordingHandler handler, string domesticApiKey = "test-key")
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://mkpservice.shippop.test/")
        };
        var options = Options.Create(new ShippopOptions
        {
            DomesticBaseUrl = "https://mkpservice.shippop.test",
            DomesticApiKey = domesticApiKey
        });

        return new ShippopShippingGatewayService(httpClient, options);
    }

    private static ShippingRateRequest CreateRateRequest()
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
            CourierCodes = ["EMST"]
        };
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
