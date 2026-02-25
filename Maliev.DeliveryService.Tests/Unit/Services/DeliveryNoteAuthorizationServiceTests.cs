using Maliev.DeliveryService.Api.Services;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Services;

public class DeliveryNoteAuthorizationServiceTests
{
    private readonly DeliveryNoteAuthorizationService _service;

    public DeliveryNoteAuthorizationServiceTests()
    {
        _service = new DeliveryNoteAuthorizationService();
    }

    [Fact]
    public async Task CanAccessCustomerAsync_ReturnsTrue()
    {
        // Act
        var result = await _service.CanAccessCustomerAsync("user", Guid.NewGuid());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetAuthorizedCustomerIdsAsync_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetAuthorizedCustomerIdsAsync("user");

        // Assert
        Assert.Empty(result);
    }
}
