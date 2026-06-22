using Maliev.DeliveryService.Api.Configuration;
using Maliev.DeliveryService.Infrastructure.Shipping;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Maliev.DeliveryService.Tests.Unit.Configuration;

public class ShippopOptionsValidatorTests
{
    [Fact]
    public void Validate_InDevelopment_AllowsShippopDevHosts()
    {
        IValidateOptions<ShippopOptions> validator = new ShippopOptionsValidator(new TestHostEnvironment("Development"));

        var result = validator.Validate(null, new ShippopOptions
        {
            DomesticBaseUrl = "https://mkpservice.shippop.dev",
            InternationalBaseUrl = "https://inter.shippop.dev"
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_InDevelopment_RejectsNonDevShippopHosts()
    {
        IValidateOptions<ShippopOptions> validator = new ShippopOptionsValidator(new TestHostEnvironment("Development"));

        var result = validator.Validate(null, new ShippopOptions
        {
            DomesticBaseUrl = "https://mkpservice.shippop.com",
            InternationalBaseUrl = "https://inter.shippop.com"
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("DomesticBaseUrl", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("InternationalBaseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_InProduction_DoesNotApplyDevelopmentEndpointGuard()
    {
        IValidateOptions<ShippopOptions> validator = new ShippopOptionsValidator(new TestHostEnvironment("Production"));

        var result = validator.Validate(null, new ShippopOptions
        {
            DomesticBaseUrl = "https://mkpservice.shippop.com",
            InternationalBaseUrl = "https://inter.shippop.com"
        });

        Assert.True(result.Succeeded);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Maliev.DeliveryService.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
