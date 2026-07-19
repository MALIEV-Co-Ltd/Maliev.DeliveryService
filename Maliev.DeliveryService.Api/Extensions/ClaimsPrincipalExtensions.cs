using System.Security.Claims;

namespace Maliev.DeliveryService.Api.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the claims principal.
    /// </summary>
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("User ID not found in claims");
    }

    /// <summary>
    /// Gets the principal ID from the claims principal.
    /// </summary>
    public static string GetPrincipalId(this ClaimsPrincipal principal) => GetUserId(principal);

    /// <summary>
    /// Gets the customer IDs from the claims principal.
    /// </summary>
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
