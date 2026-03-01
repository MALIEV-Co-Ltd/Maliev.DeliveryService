using Moq;
using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Infrastructure.Authorization;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Services;

public class DeliveryNoteAuthorizationServiceTests
{
    private readonly DeliveryNoteAuthorizationService _service;

    public DeliveryNoteAuthorizationServiceTests()
    {
        var mockIamClient = new Moq.Mock<Maliev.Aspire.ServiceDefaults.IAM.IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(Moq.It.IsAny<string>(), Moq.It.IsAny<string>(), Moq.It.IsAny<string>(), Moq.It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockIamClient.Setup(x => x.GetAuthorizedResourcesAsync(Moq.It.IsAny<string>(), Moq.It.IsAny<string>(), Moq.It.IsAny<string>(), Moq.It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>());
        _service = new DeliveryNoteAuthorizationService(mockIamClient.Object);
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
