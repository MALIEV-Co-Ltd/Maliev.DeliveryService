using Maliev.DeliveryService.Infrastructure.Shipping;
using Microsoft.Extensions.Options;

namespace Maliev.DeliveryService.Api.Configuration;

internal sealed class ShippopOptionsValidator(IHostEnvironment environment) : IValidateOptions<ShippopOptions>
{
    ValidateOptionsResult IValidateOptions<ShippopOptions>.Validate(string? name, ShippopOptions options)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        ValidateDevHost(options.DomesticBaseUrl, "Shippop:DomesticBaseUrl", required: true, failures);
        ValidateDevHost(options.InternationalBaseUrl, "Shippop:InternationalBaseUrl", required: false, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateDevHost(string? value, string key, bool required, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                failures.Add($"{key} is required in development and testing.");
            }

            return;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !uri.Host.EndsWith(".shippop.dev", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{key} must use a SHIPPOP .dev HTTPS host in development and testing.");
        }
    }
}
