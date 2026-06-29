using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Application.Services;
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
            new ShippingPackagePlanner(),
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

        var courier = Assert.Single(result);
        Assert.Equal("kerry", courier.CourierCode);
        Assert.Equal("GoShip", courier.Provider);
        Assert.NotNull(courier.LogoUrl);
    }

    [Fact]
    public async Task GetCouriers_WhenShippopReturnsEmpty_FallsBackToGoShip()
    {
        _shippop.Setup(x => x.GetCouriersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var goShipCouriers = new List<ShippingCourierResponse> { new() { CourierCode = "kerry", Provider = "GoShip" } };
        _goShip.Setup(x => x.GetCouriersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(goShipCouriers);

        var result = await _service.GetCouriersAsync(CancellationToken.None);

        var courier = Assert.Single(result);
        Assert.Equal("kerry", courier.CourierCode);
        Assert.Equal("GoShip", courier.Provider);
        Assert.NotNull(courier.LogoUrl);
    }

    [Fact]
    public async Task GetRates_WhenShippopSucceeds_ReturnsShippopResultWithoutCallingGoShip()
    {
        var shippopRates = new List<ShippingRateOptionResponse> { new() { CourierCode = "EMST", Price = 37m, Provider = "Shippop" } };
        _shippop.Setup(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(shippopRates);

        var result = await _service.GetRatesAsync(new ShippingRateRequest(), CancellationToken.None);

        var rate = Assert.Single(result);
        Assert.Equal("EMST", rate.CourierCode);
        Assert.Equal(37m, rate.Price);
        Assert.Equal(1, rate.PackageCount);
        Assert.NotNull(rate.CourierLogoUrl);
        Assert.Single(rate.Packages);
        _goShip.Verify(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRates_WhenShippopThrows_FallsBackToGoShip()
    {
        _shippop.Setup(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SHIPPOP domestic API key is not configured."));
        var goShipRates = new List<ShippingRateOptionResponse> { new() { CourierCode = "kerry", Price = 82m, Provider = "GoShip" } };
        _goShip.Setup(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(goShipRates);

        var result = await _service.GetRatesAsync(new ShippingRateRequest(), CancellationToken.None);

        var rate = Assert.Single(result);
        Assert.Equal("kerry", rate.CourierCode);
        Assert.Equal(82m, rate.Price);
    }

    [Fact]
    public async Task GetRates_WhenShippopReturnsEmpty_FallsBackToGoShip()
    {
        _shippop.Setup(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var goShipRates = new List<ShippingRateOptionResponse> { new() { CourierCode = "kerry", Price = 82m, Provider = "GoShip" } };
        _goShip.Setup(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(goShipRates);

        var result = await _service.GetRatesAsync(new ShippingRateRequest(), CancellationToken.None);

        var rate = Assert.Single(result);
        Assert.Equal("kerry", rate.CourierCode);
        Assert.Equal(82m, rate.Price);
    }

    [Fact]
    public async Task GetRates_WithManyProjectParts_SplitsPackagesAndAggregatesCourierPrice()
    {
        var requestedParcels = new List<ShippingParcelRequest>();
        _shippop
            .Setup(x => x.GetRatesAsync(It.IsAny<ShippingRateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingRateRequest request, CancellationToken _) =>
            {
                requestedParcels.Add(request.Parcel);
                return
                [
                    new ShippingRateOptionResponse
                    {
                        CourierCode = "FLE",
                        CourierName = "Flash Express",
                        Price = request.Parcel.Weight > 10_000m ? 180m : 95m,
                        Currency = "THB",
                        EstimatedDelivery = "1-2 days",
                        Provider = "Shippop"
                    },
                    new ShippingRateOptionResponse
                    {
                        CourierCode = "DHL",
                        CourierName = "DHL",
                        Price = 240m,
                        Currency = "THB",
                        EstimatedDelivery = "next day",
                        Provider = "Shippop"
                    }
                ];
            });

        var result = await _service.GetRatesAsync(CreateBulkPartRateRequest(), CancellationToken.None);

        Assert.True(requestedParcels.Count > 1);
        Assert.All(requestedParcels, parcel =>
        {
            Assert.True(parcel.Weight <= 20_000m);
            Assert.True(parcel.Length <= 60m);
            Assert.True(parcel.Width <= 45m);
            Assert.True(parcel.Height <= 45m);
        });

        var flash = result.First(rate => rate.CourierCode == "FLE");
        Assert.Equal(requestedParcels.Count, flash.PackageCount);
        Assert.Equal(requestedParcels.Count, flash.Packages.Count);
        Assert.Equal(flash.Packages.Sum(package => package.Price), flash.Price);
        Assert.Equal(flash.Packages.Sum(package => package.Weight), flash.TotalWeight);
        Assert.Equal("1-2 days", flash.EstimatedDelivery);
        Assert.NotNull(flash.CourierLogoUrl);
        Assert.All(flash.Packages, package => Assert.NotEmpty(package.Items));
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

    private static ShippingRateRequest CreateBulkPartRateRequest()
    {
        return new ShippingRateRequest
        {
            From = new ShippingAddressRequest
            {
                Name = "MALIEV",
                Address = "MALIEV",
                District = "Pathum Wan",
                State = "Pathum Wan",
                Province = "Bangkok",
                Postcode = "10400",
                Tel = "020000000"
            },
            To = new ShippingAddressRequest
            {
                Name = "Customer",
                Address = "Dock 2",
                District = "Bang Rak",
                State = "Bang Rak",
                Province = "Bangkok",
                Postcode = "10500",
                Tel = "0800000000"
            },
            Parcel = new ShippingParcelRequest
            {
                Name = "Manual fallback",
                Weight = 1,
                Length = 1,
                Width = 1,
                Height = 1
            },
            Parts =
            [
                new ShippingPackagePartRequest
                {
                    Name = "Printed bracket",
                    Quantity = 1_000,
                    Weight = 45m,
                    Length = 12m,
                    Width = 8m,
                    Height = 4m
                },
                new ShippingPackagePartRequest
                {
                    Name = "Machined cover",
                    Quantity = 8,
                    Weight = 350m,
                    Length = 24m,
                    Width = 16m,
                    Height = 6m,
                    PackagingMargin = 2m
                }
            ]
        };
    }
}
