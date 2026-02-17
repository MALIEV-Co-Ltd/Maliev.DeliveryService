using System.Security.Claims;

namespace Maliev.DeliveryService.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("User ID not found in claims");
    }

    public static string GetPrincipalId(this ClaimsPrincipal principal) => GetUserId(principal);

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
