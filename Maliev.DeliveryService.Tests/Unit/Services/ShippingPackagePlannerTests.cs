using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Application.Services;

namespace Maliev.DeliveryService.Tests.Unit.Services;

public sealed class ShippingPackagePlannerTests
{
    private readonly ShippingPackagePlanner _planner = new();

    [Fact]
    public void PlanPackages_WithSinglePart_UsesBoundingBoxPlusPackagingMargins()
    {
        var request = CreateRequest(
            new ShippingPackagePartRequest
            {
                Name = "Single cover",
                Quantity = 1,
                Weight = 500m,
                Length = 20m,
                Width = 10m,
                Height = 5m
            });

        var package = Assert.Single(_planner.PlanPackages(request));

        Assert.Equal(1, package.PackageNumber);
        Assert.Equal(760m, package.Weight);
        Assert.Equal(29m, package.Length);
        Assert.Equal(19m, package.Width);
        Assert.Equal(14m, package.Height);
        var item = Assert.Single(package.Items);
        Assert.Equal("Single cover", item.Name);
        Assert.Equal(1, item.Quantity);
        Assert.Equal(23m, item.UnitLength);
        Assert.Equal(13m, item.UnitWidth);
        Assert.Equal(8m, item.UnitHeight);
    }

    [Fact]
    public void PlanPackages_WithMultipleSmallParts_CombinesIntoOneBox()
    {
        var packages = _planner.PlanPackages(CreateRequest(
            new ShippingPackagePartRequest
            {
                Name = "Spacer",
                Quantity = 6,
                Weight = 100m,
                Length = 8m,
                Width = 5m,
                Height = 2m
            },
            new ShippingPackagePartRequest
            {
                Name = "Bracket",
                Quantity = 2,
                Weight = 180m,
                Length = 12m,
                Width = 6m,
                Height = 3m
            }));

        var package = Assert.Single(packages);
        Assert.Equal(1_290m, package.Weight);
        Assert.Equal(8, package.Items.Sum(item => item.Quantity));
        Assert.True(package.Length <= 60m);
        Assert.True(package.Width <= 45m);
        Assert.True(package.Height <= 45m);
    }

    [Fact]
    public void PlanPackages_WithLargeQuantity_SplitsIntoMultipleBoxes()
    {
        var packages = _planner.PlanPackages(CreateRequest(
            new ShippingPackagePartRequest
            {
                Name = "Production run clip",
                Quantity = 1_000,
                Weight = 45m,
                Length = 12m,
                Width = 8m,
                Height = 4m
            }));

        Assert.True(packages.Count > 1);
        Assert.All(packages, package =>
        {
            Assert.True(package.Weight <= 20_000m);
            Assert.True(package.Length <= 60m);
            Assert.True(package.Width <= 45m);
            Assert.True(package.Height <= 45m);
            Assert.NotEmpty(package.Items);
        });
        Assert.Equal(1_000, packages.Sum(package => package.Items.Sum(item => item.Quantity)));
    }

    private static ShippingRateRequest CreateRequest(params ShippingPackagePartRequest[] parts)
    {
        return new ShippingRateRequest
        {
            Parcel = new ShippingParcelRequest
            {
                Name = "Manual fallback",
                Weight = 1,
                Length = 1,
                Width = 1,
                Height = 1
            },
            Packaging = new ShippingPackagingOptionsRequest
            {
                PartMargin = 1.5m,
                BoxMargin = 3m,
                PartPackagingWeight = 10m,
                BoxPackagingWeight = 250m,
                MaxPackageWeight = 20_000m,
                MaxPackageLength = 60m,
                MaxPackageWidth = 45m,
                MaxPackageHeight = 45m
            },
            Parts = parts.ToList()
        };
    }
}
