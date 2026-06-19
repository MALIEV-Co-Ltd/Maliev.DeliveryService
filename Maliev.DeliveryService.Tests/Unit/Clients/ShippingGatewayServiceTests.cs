using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Infrastructure.Shipping;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.DeliveryService.Tests.Unit.Clients;

public class ShippingGatewayServiceTests
{
    private readonly Mock<IShippopShippingGatewayService> _shippop = new();
    private readonly Mock<IGoShipShippingGatewayService> _goShip = new();
    private readonly ShippingGatewayService _service;

    public ShippingGatewayServiceTests()
    {
        _service = new ShippingGatewayService(
            _shippop.Object,
            _goShip.Object,
            Mock.Of<ILogger<ShippingGatewayService>>());
    }

    [Fact]
    public async Task GetCouriers_WhenShippopSucceeds_ReturnsShippopResultWithoutCallingGoShip()
    {
        var shippopCouriers = new List<ShippingCourierResponse> { new() { CourierCode = "EMST", Provider = "Shippop" } };
        _shippop.Setup(x => x.GetCouriersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(shippopCouriers);

        var result = await _service.GetCouriersAsync(CancellationToken.None);

        Assert.Equal(shippopCouriers, result);
        _goShip.Verify(x => x.GetCouriersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCouriers_WhenShippopThrows_FallsBackToGoShip()
    {
        _shippop.Setup(x => x.GetCouriersAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("down"));
        var goShipCouriers = new List<ShippingCourierResponse> { new() { CourierCode = "kerry", Provider = "GoShip" } };
        _goShip.Setup(x => x.GetCouriersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(goShipCouriers);

        var result = await _service.GetCouriersAsync(CancellationToken.None);

        Assert.Same(goShipCouriers, result);
    }

    [Fact]
    public async Task GetCouriers_WhenShippopReturnsEmpty_FallsBackToGoShip()
    {
        _shippop.Setup(x => x.GetCouriersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var goShipCouriers = new List<ShippingCourierResponse> { new() { CourierCode = "kerry", Provider = "GoShip" } };
        _goShip.Setup(x => x.GetCouriersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(goShipCouriers);

        var result = await _service.GetCouriersAsync(CancellationToken.None);

        Assert.Same(goShipCouriers, result);
    }

    [Fact]
    public async Task GetRates_WhenShippopSucceeds_ReturnsShippopResultWithoutCallingGoShip()
    {
        var shippopRates = new List<ShippingRateOptionResponse> { new() { CourierCode = "EMST", Provider = "Shippop" } };
        _shippop.Setup(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(shippopRates);

        var result = await _service.GetRatesAsync(new ShippingRateRequest(), CancellationToken.None);

        Assert.Equal(shippopRates, result);
        _goShip.Verify(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRates_WhenShippopThrows_FallsBackToGoShip()
    {
        _shippop.Setup(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SHIPPOP domestic API key is not configured."));
        var goShipRates = new List<ShippingRateOptionResponse> { new() { CourierCode = "kerry", Provider = "GoShip" } };
        _goShip.Setup(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(goShipRates);

        var result = await _service.GetRatesAsync(new ShippingRateRequest(), CancellationToken.None);

        Assert.Same(goShipRates, result);
    }

    [Fact]
    public async Task GetRates_WhenShippopReturnsEmpty_FallsBackToGoShip()
    {
        _shippop.Setup(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var goShipRates = new List<ShippingRateOptionResponse> { new() { CourierCode = "kerry", Provider = "GoShip" } };
        _goShip.Setup(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(goShipRates);

        var result = await _service.GetRatesAsync(new ShippingRateRequest(), CancellationToken.None);

        Assert.Same(goShipRates, result);
    }

    [Fact]
    public async Task GetTracking_WhenShippopReturnsKnownStatus_ReturnsShippopResultWithoutCallingGoShip()
    {
        var shippopTracking = new ShippingTrackingResponse { TrackingCode = "SP1", Status = "delivered", Provider = "Shippop" };
        _shippop.Setup(x => x.GetTrackingAsync("SP1", It.IsAny<CancellationToken>())).ReturnsAsync(shippopTracking);

        var result = await _service.GetTrackingAsync("SP1", CancellationToken.None);

        Assert.Same(shippopTracking, result);
        _goShip.Verify(x => x.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTracking_WhenShippopThrows_FallsBackToGoShip()
    {
        _shippop.Setup(x => x.GetTrackingAsync("GS1", It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("down"));
        var goShipTracking = new ShippingTrackingResponse { TrackingCode = "GS1", Status = "in_transit", Provider = "GoShip" };
        _goShip.Setup(x => x.GetTrackingAsync("GS1", It.IsAny<CancellationToken>())).ReturnsAsync(goShipTracking);

        var result = await _service.GetTrackingAsync("GS1", CancellationToken.None);

        Assert.Same(goShipTracking, result);
    }

    [Fact]
    public async Task GetTracking_WhenShippopReturnsUnknownStatus_FallsBackToGoShip()
    {
        var shippopTracking = new ShippingTrackingResponse { TrackingCode = "X1", Status = "unknown", Provider = "Shippop" };
        _shippop.Setup(x => x.GetTrackingAsync("X1", It.IsAny<CancellationToken>())).ReturnsAsync(shippopTracking);
        var goShipTracking = new ShippingTrackingResponse { TrackingCode = "X1", Status = "in_transit", Provider = "GoShip" };
        _goShip.Setup(x => x.GetTrackingAsync("X1", It.IsAny<CancellationToken>())).ReturnsAsync(goShipTracking);

        var result = await _service.GetTrackingAsync("X1", CancellationToken.None);

        Assert.Same(goShipTracking, result);
    }

    [Fact]
    public async Task GetTracking_WhenTrackingCodeIsEmpty_ThrowsWithoutCallingEitherGateway()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetTrackingAsync(" ", CancellationToken.None));

        _shippop.Verify(x => x.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _goShip.Verify(x => x.GetTrackingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
