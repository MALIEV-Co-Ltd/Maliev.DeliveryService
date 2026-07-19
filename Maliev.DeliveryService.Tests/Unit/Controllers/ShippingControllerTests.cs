using Maliev.DeliveryService.Api.Controllers;
using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.DeliveryService.Tests.Unit.Controllers;

public class ShippingControllerTests
{
    private readonly Mock<IShippingGatewayService> _shippingGatewayService = new();
    private readonly ShippingController _controller;

    public ShippingControllerTests()
    {
        _controller = new ShippingController(
            _shippingGatewayService.Object,
            Mock.Of<ILogger<ShippingController>>());
    }

    [Fact]
    public async Task GetCouriers_ReturnsGatewayCouriers()
    {
        var couriers = new List<ShippingCourierResponse>
        {
            new() { CourierCode = "EMST", CourierName = "Thailand Post EMS" }
        };
        _shippingGatewayService
            .Setup(x => x.GetCouriersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(couriers);

        var result = await _controller.GetCouriers(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(couriers, okResult.Value);
    }

    [Fact]
    public async Task GetRates_WhenGatewayUnavailable_ReturnsServiceUnavailable()
    {
        _shippingGatewayService
            .Setup(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SHIPPOP domestic API key is not configured."));

        var result = await _controller.GetRates(new ShippingRateRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetTracking_WhenTrackingCodeIsInvalid_ReturnsBadRequest()
    {
        _shippingGatewayService
            .Setup(x => x.GetTrackingAsync(" ", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Tracking code is required."));

        var result = await _controller.GetTracking(" ", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
