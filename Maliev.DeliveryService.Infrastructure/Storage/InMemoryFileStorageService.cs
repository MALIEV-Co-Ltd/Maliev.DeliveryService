using System.Collections.Concurrent;
using Maliev.DeliveryService.Application.Abstractions;

namespace Maliev.DeliveryService.Infrastructure.Storage;

/// <summary>
/// In-memory file storage implementation for integration and AppHost system tests.
/// </summary>
public class InMemoryFileStorageService : IFileStorageService
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    /// <inheritdoc />
    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, ct);

        var storageUrl = $"memory://delivery/{Guid.NewGuid():N}/{fileName}";
        _files[storageUrl] = memoryStream.ToArray();

        return storageUrl;
    }

    /// <inheritdoc />
    public Task<string> GetSignedUrlAsync(
        string fileName,
        TimeSpan expiration,
        CancellationToken ct = default)
    {
        return Task.FromResult($"memory://delivery/{Uri.EscapeDataString(fileName)}?signed=true");
    }

    /// <inheritdoc />
    public Task DeleteAsync(string fileName, CancellationToken ct = default)
    {
        _files.TryRemove(fileName, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string fileName, CancellationToken ct = default)
    {
        return Task.FromResult(_files.ContainsKey(fileName));
    }
}
