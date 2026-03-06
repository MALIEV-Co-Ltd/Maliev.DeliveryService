using Moq;
using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Infrastructure.Authorization;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Services;

public class DeliveryNoteAuthorizationServiceTests
{
    private readonly Mock<Maliev.Aspire.ServiceDefaults.IAM.IIamServiceClient> _mockIamClient;
    private readonly DeliveryNoteAuthorizationService _service;

    public DeliveryNoteAuthorizationServiceTests()
    {
        _mockIamClient = new Mock<Maliev.Aspire.ServiceDefaults.IAM.IIamServiceClient>();
        _service = new DeliveryNoteAuthorizationService(_mockIamClient.Object);
    }

    [Fact]
    public async Task CanAccessCustomerAsync_ReturnsTrue()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _mockIamClient.Setup(x => x.CheckPermissionAsync("user", "delivery.customer.read", $"customers/{customerId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CanAccessCustomerAsync("user", customerId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessCustomerAsync_ReturnsFalse()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _mockIamClient.Setup(x => x.CheckPermissionAsync("user", "delivery.customer.read", $"customers/{customerId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CanAccessCustomerAsync("user", customerId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAuthorizedCustomerIdsAsync_ReturnsParsedGuids()
    {
        // Arrange
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();
        _mockIamClient.Setup(x => x.GetAuthorizedResourcesAsync("user", "delivery.customer.read", "customers", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { customerId1.ToString(), customerId2.ToString(), "invalid" });

        // Act
        var result = await _service.GetAuthorizedCustomerIdsAsync("user");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(customerId1, result);
        Assert.Contains(customerId2, result);
    }

    [Fact]
    public async Task GetAuthorizedCustomerIdsAsync_ReturnsEmptyList_WhenNoResources()
    {
        // Arrange
        _mockIamClient.Setup(x => x.GetAuthorizedResourcesAsync("user", "delivery.customer.read", "customers", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _service.GetAuthorizedCustomerIdsAsync("user");

        // Assert
        Assert.Empty(result);
    }
}
