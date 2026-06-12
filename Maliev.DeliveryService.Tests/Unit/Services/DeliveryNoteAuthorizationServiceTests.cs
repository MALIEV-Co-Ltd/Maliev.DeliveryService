using Moq;
using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.Authorization;
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
    public async Task CanAccessCustomerAsync_SystemAutoPrincipal_ReturnsTrueWithoutIamCall()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var result = await _service.CanAccessCustomerAsync("system-auto", customerId);

        // Assert
        Assert.True(result);
        _mockIamClient.Verify(x => x.CheckPermissionAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
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
    public async Task HasUnrestrictedAccessAsync_UsesDeliveryNoteReadWildcardResource()
    {
        // Arrange
        _mockIamClient.Setup(x => x.CheckPermissionAsync(
                "user",
                DeliveryPermissions.DeliveryNoteRead,
                "delivery-notes/*",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.HasUnrestrictedAccessAsync("user");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasUnrestrictedAccessAsync_SystemAutoPrincipal_ReturnsTrueWithoutIamCall()
    {
        // Act
        var result = await _service.HasUnrestrictedAccessAsync("system-auto");

        // Assert
        Assert.True(result);
        _mockIamClient.Verify(x => x.CheckPermissionAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
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
