using Google.Cloud.Storage.V1;
using Maliev.DeliveryService.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Services;

public class GoogleCloudStorageServiceTests
{
    private readonly Mock<StorageClient> _mockStorageClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<GoogleCloudStorageService>> _mockLogger;
    private readonly GoogleCloudStorageService _service;
    private readonly GoogleCloudStorageService _serviceWithSigner;

    public GoogleCloudStorageServiceTests()
    {
        _mockStorageClient = new Mock<StorageClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<GoogleCloudStorageService>>();

        _mockConfiguration.Setup(x => x["GoogleCloudStorage:BucketName"]).Returns("test-bucket");

        _service = new GoogleCloudStorageService(
            _mockStorageClient.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        _serviceWithSigner = new GoogleCloudStorageService(
            _mockStorageClient.Object,
            "test-bucket",
            _mockLogger.Object,
            (b, o, e, m) => $"https://signed-url/{b}/{o}");
    }

    [Fact]
    public async Task UploadAsync_ValidFile_ReturnsGsUrl()
    {
        // Arrange
        var stream = new MemoryStream();
        var fileName = "test.txt";
        var contentType = "text/plain";

        _mockStorageClient.Setup(x => x.UploadObjectAsync(
            "test-bucket",
            It.IsAny<string>(),
            contentType,
            stream,
            It.IsAny<UploadObjectOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Google.Apis.Storage.v1.Data.Object());

        // Act
        var result = await _service.UploadAsync(stream, fileName, contentType);

        // Assert
        Assert.StartsWith("gs://test-bucket/", result);
        Assert.Contains(fileName, result);
    }

    [Fact]
    public async Task GetSignedUrlAsync_ValidFile_ReturnsUrl()
    {
        // Act
        var result = await _serviceWithSigner.GetSignedUrlAsync("gs://test-bucket/test.txt", TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal("https://signed-url/test-bucket/test.txt", result);
    }

    [Fact]
    public async Task ExistsAsync_ObjectExists_ReturnsTrue()
    {
        // Arrange
        var fileName = "test.txt";
        _mockStorageClient.Setup(x => x.GetObjectAsync(
            "test-bucket",
            It.IsAny<string>(),
            It.IsAny<GetObjectOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Google.Apis.Storage.v1.Data.Object());

        // Act
        var result = await _service.ExistsAsync(fileName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_ObjectNotFound_ReturnsFalse()
    {
        // Arrange
        var fileName = "test.txt";
        _mockStorageClient.Setup(x => x.GetObjectAsync(
            "test-bucket",
            It.IsAny<string>(),
            It.IsAny<GetObjectOptions>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateNotFoundException());

        // Act
        var result = await _service.ExistsAsync(fileName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ValidFile_CallsDelete()
    {
        // Arrange
        var fileName = "gs://test-bucket/some/path/test.txt";
        _mockStorageClient.Setup(x => x.DeleteObjectAsync(
            "test-bucket",
            "some/path/test.txt",
            It.IsAny<DeleteObjectOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(fileName);

        // Assert
        _mockStorageClient.Verify(x => x.DeleteObjectAsync("test-bucket", "some/path/test.txt", It.IsAny<DeleteObjectOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithoutGsPrefix_CallsDelete()
    {
        // Arrange
        var fileName = "some/path/test.txt";
        _mockStorageClient.Setup(x => x.DeleteObjectAsync(
            "test-bucket",
            "some/path/test.txt",
            It.IsAny<DeleteObjectOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(fileName);

        // Assert
        _mockStorageClient.Verify(x => x.DeleteObjectAsync("test-bucket", "some/path/test.txt", It.IsAny<DeleteObjectOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsGracefully()
    {
        // Arrange
        var fileName = "missing.txt";
        _mockStorageClient.Setup(x => x.DeleteObjectAsync(
            "test-bucket",
            It.IsAny<string>(),
            It.IsAny<DeleteObjectOptions>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateNotFoundException());

        // Act
        await _service.DeleteAsync(fileName);

        // Assert
        // Should not throw
        _mockStorageClient.Verify(x => x.DeleteObjectAsync("test-bucket", It.IsAny<string>(), It.IsAny<DeleteObjectOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_UnexpectedError_Throws()
    {
        // Arrange
        var fileName = "error.txt";
        _mockStorageClient.Setup(x => x.DeleteObjectAsync(
            "test-bucket",
            It.IsAny<string>(),
            It.IsAny<DeleteObjectOptions>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("BOOM"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.DeleteAsync(fileName));
    }

    private Google.GoogleApiException CreateNotFoundException()
    {
        var ex = new Google.GoogleApiException("service", "Not Found");
        var property = typeof(Google.GoogleApiException).GetProperty("HttpStatusCode");
        if (property != null && property.CanWrite)
        {
            property.SetValue(ex, HttpStatusCode.NotFound);
        }
        return ex;
    }
}
