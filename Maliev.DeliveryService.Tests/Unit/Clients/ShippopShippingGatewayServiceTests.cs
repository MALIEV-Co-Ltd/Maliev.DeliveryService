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
        Assert.Equal("TH", shipment.GetProperty("to").GetProperty("country_code").GetString());
        Assert.Equal("EMST", shipment.GetProperty("courier_code").GetString());
        var rate = Assert.Single(rates);
        Assert.Equal("EMST", rate.CourierCode);
        Assert.Equal(37m, rate.Price);
    }

    [Fact]
    public async Task GetRates_WithMultipleCourierCodes_SendsOnePricelistItemPerCourier()
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
                },
                "1": {
                  "DHL": {
                    "courier_code": "DHL",
                    "courier_name": "DHL",
                    "price": 52
                  }
                }
              }
            }
            """);
        var request = CreateRateRequest();
        request.CourierCodes = ["EMST", "DHL"];
        var service = CreateService(handler);

        var rates = await service.GetRatesAsync(request, CancellationToken.None);

        using var document = JsonDocument.Parse(handler.Body);
        var data = document.RootElement.GetProperty("data");
        Assert.Equal("EMST", data.GetProperty("0").GetProperty("courier_code").GetString());
        Assert.Equal("DHL", data.GetProperty("1").GetProperty("courier_code").GetString());
        Assert.Equal(2, rates.Count);
        Assert.Contains(rates, rate => rate.CourierCode == "EMST" && rate.Price == 37m);
        Assert.Contains(rates, rate => rate.CourierCode == "DHL" && rate.Price == 52m);
    }

    [Fact]
    public async Task GetRates_ForInternationalPublicRates_UsesShippopInterPublicPriceEndpoint()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.OK,
            """
            {
              "couriers": [
                {
                  "id": 2,
                  "name": "Aramex - PPX",
                  "duration": 4,
                  "price": "609.00",
                  "code": "aramex_ppx",
                  "ref": "CRARMPPX",
                  "type": "pick_up",
                  "error_code": null
                },
                {
                  "id": 5,
                  "name": "Thai Post - ePacket",
                  "price": null,
                  "code": "thai_post_epackage",
                  "error_code": "notSupport.weight"
                }
              ]
            }
            """);
        var request = CreateRateRequest();
        request.UsePublicRates = true;
        request.To.CountryCode = "au";
        var service = CreateService(handler, domesticApiKey: "");

        var rates = await service.GetRatesAsync(request, CancellationToken.None);

        Assert.Equal("https://inter.shippop.test/api/public/courier/price", handler.RequestUri?.ToString());
        using var document = JsonDocument.Parse(handler.Body);
        Assert.Equal(500, document.RootElement.GetProperty("weight").GetInt32());
        Assert.Equal("AU", document.RootElement.GetProperty("country_code").GetString());
        Assert.True(document.RootElement.GetProperty("show_all").GetBoolean());
        var rate = Assert.Single(rates);
        Assert.Equal("aramex_ppx", rate.CourierCode);
        Assert.Equal("Aramex - PPX", rate.CourierName);
        Assert.Equal(609m, rate.Price);
        Assert.Equal("THB", rate.Currency);
        Assert.Equal("pick_up", rate.ServiceLevel);
        Assert.Equal("4", rate.EstimatedDelivery);
        Assert.Equal("Shippop", rate.Provider);
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
              "tracking_data": {
                "trackings": [
                  {
                    "value": "Bangkok EMS Centre",
                    "occurred_date": "2026-06-19T10:00:00Z",
                    "tracking": {
                      "name": "Final delivery",
                      "courier_message": "Delivered to recipient"
                    }
                  }
                ],
                "shipment": {
                  "courier_tracking_code": "SP529189074",
                  "shipment": {
                    "status": "complete",
                    "tracking_code": "INT00000307"
                  },
                  "order": {
                    "courier": {
                      "code": "EMST",
                      "name": "Thailand Post EMS"
                    }
                  }
                }
              }
            }
            """);
        var service = CreateService(handler);

        var tracking = await service.GetTrackingAsync("SP529189074", CancellationToken.None);

        Assert.Equal("https://inter.shippop.test/api/public/tracking/detail/SP529189074", handler.RequestUri?.ToString());
        Assert.Equal(string.Empty, handler.Body);
        Assert.Equal("SP529189074", tracking.TrackingCode);
        Assert.Equal("complete", tracking.Status);
        var trackingEvent = Assert.Single(tracking.Events);
        Assert.Equal("Final delivery", trackingEvent.Status);
        Assert.Equal("Bangkok EMS Centre", trackingEvent.Location);
        Assert.Equal("Delivered to recipient", trackingEvent.Description);
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
            DomesticApiKey = domesticApiKey,
            InternationalBaseUrl = "https://inter.shippop.test"
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
