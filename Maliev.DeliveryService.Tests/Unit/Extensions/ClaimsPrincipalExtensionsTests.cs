using System.Security.Claims;
using Maliev.DeliveryService.Api.Extensions;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_WithSubClaim_ReturnsValue()
    {
        // Arrange
        var claims = new[] { new Claim("sub", "user-123") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserId();

        // Assert
        Assert.Equal("user-123", result);
    }

    [Fact]
    public void GetUserId_WithNameIdentifierClaim_ReturnsValue()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-456") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserId();

        // Assert
        Assert.Equal("user-456", result);
    }

    [Fact]
    public void GetUserId_NoClaims_ThrowsInvalidOperationException()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => principal.GetUserId());
    }

    [Fact]
    public void GetCustomerIds_WithCustomerClaims_ReturnsList()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("customer_id", id1.ToString()),
            new Claim("customer_id", id2.ToString()),
            new Claim("customer_id", "invalid-guid")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetCustomerIds();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(id1, result);
        Assert.Contains(id2, result);
    }
}
