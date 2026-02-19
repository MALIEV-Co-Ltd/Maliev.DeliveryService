using System.Security.Claims;
using System.Text.Encodings.Web;
using Maliev.DeliveryService.Api.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maliev.DeliveryService.Tests.Integration.TestFixtures;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "test-user"),
            new("sub", "test-user-id"),
            new("customer_id", Guid.NewGuid().ToString())
        };

        // Add all delivery permissions so authorization checks pass
        foreach (var permission in DeliveryPermissions.All)
        {
            claims.Add(new Claim("permissions", permission));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
