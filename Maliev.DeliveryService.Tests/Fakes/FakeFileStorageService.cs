using Maliev.DeliveryService.Api.Services;

namespace Maliev.DeliveryService.Tests.Fakes;

/// <summary>
/// Fake implementation of IFileStorageService for testing (no mocking libraries)
/// </summary>
public class FakeFileStorageService : IFileStorageService
{
    private readonly Dictionary<string, byte[]> _files = new();

    public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        var fileData = memoryStream.ToArray();

        var storageUrl = $"gs://fake-bucket/{Guid.NewGuid()}/{fileName}";
        _files[storageUrl] = fileData;

        return Task.FromResult(storageUrl);
    }

    public Task<string> GetSignedUrlAsync(string fileName, TimeSpan expiration, CancellationToken ct = default)
    {
        var signedUrl = $"https://storage.googleapis.com/fake-bucket/{fileName}?signed=true";
        return Task.FromResult(signedUrl);
    }

    public Task DeleteAsync(string fileName, CancellationToken ct = default)
    {
        _files.Remove(fileName);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string fileName, CancellationToken ct = default)
    {
        return Task.FromResult(_files.ContainsKey(fileName));
    }

    // Test helper methods
    public byte[]? GetFileData(string storageUrl)
    {
        _files.TryGetValue(storageUrl, out var data);
        return data;
    }

    public void Clear()
    {
        _files.Clear();
    }
}
