namespace Maliev.DeliveryService.Application.Services;

/// <summary>
/// Resolves courier logo URLs from known carrier codes and names.
/// </summary>
public static class CourierLogoCatalog
{
    private static readonly Dictionary<string, string> Domains = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ARM"] = "aramex.com",
        ["ARAMEX"] = "aramex.com",
        ["BEST"] = "best-inc.co.th",
        ["DHL"] = "dhl.com",
        ["ECP"] = "thailandpost.co.th",
        ["EMS"] = "thailandpost.co.th",
        ["EMST"] = "thailandpost.co.th",
        ["FLE"] = "flashexpress.com",
        ["FLEB"] = "flashexpress.com",
        ["FLEDS"] = "flashexpress.com",
        ["FLEF"] = "flashexpress.com",
        ["FLASH"] = "flashexpress.com",
        ["JNTD"] = "jtexpress.co.th",
        ["JNTP"] = "jtexpress.co.th",
        ["J&T"] = "jtexpress.co.th",
        ["KERRY"] = "kex.co.th",
        ["KEX"] = "kex.co.th",
        ["KRYDS"] = "kex.co.th",
        ["KRYS"] = "kex.co.th",
        ["KRYX"] = "kex.co.th",
        ["LLM"] = "lalamove.com",
        ["LALAMOVE"] = "lalamove.com",
        ["LZDS"] = "lazada.co.th",
        ["MSE"] = "makesend.asia",
        ["MSEC"] = "makesend.asia",
        ["MSEF"] = "makesend.asia",
        ["SHF"] = "shippop.com",
        ["SHIPPOP"] = "shippop.com",
        ["SKT"] = "skootar.com",
        ["SKOOTAR"] = "skootar.com",
        ["SPX"] = "spx.co.th"
    };

    /// <summary>
    /// Resolves a logo URL for the supplied courier code and display name.
    /// </summary>
    public static string? ResolveLogoUrl(string? courierCode, string? courierName)
    {
        var domain = ResolveDomain(courierCode) ?? ResolveDomain(courierName);
        return domain is null
            ? null
            : $"https://www.google.com/s2/favicons?sz=64&domain={Uri.EscapeDataString(domain)}";
    }

    private static string? ResolveDomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (Domains.TryGetValue(normalized, out var domain))
        {
            return domain;
        }

        foreach (var pair in Domains)
        {
            if (normalized.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }
}
