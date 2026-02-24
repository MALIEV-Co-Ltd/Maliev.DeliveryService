using System.Security.Claims;

namespace Maliev.DeliveryService.Api.Extensions;

/// <summary>Initializes or represents a public member.</summary>
/// <summary>Initializes or represents a public member.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("User ID not found in claims");
    }

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public static string GetPrincipalId(this ClaimsPrincipal principal) => GetUserId(principal);

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public static List<Guid> GetCustomerIds(this ClaimsPrincipal principal)
    {
        var customerClaims = principal.FindAll("customer_id");
        return customerClaims
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
    }
}
