namespace Maliev.DeliveryService.Application.Abstractions;

/// <summary>
/// Service for managing file storage operations.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a file to Google Cloud Storage and return the storage URL
    /// </summary>
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    /// <summary>
    /// Get a signed URL for accessing a file in GCS
    /// </summary>
    Task<string> GetSignedUrlAsync(
        string fileName,
        TimeSpan expiration,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads a file from storage.
    /// </summary>
    Task<byte[]> DownloadAsync(string fileName, CancellationToken ct = default);

    /// <summary>
    /// Delete a file from Google Cloud Storage
    /// </summary>
    Task DeleteAsync(string fileName, CancellationToken ct = default);

    /// <summary>
    /// Check if a file exists in Google Cloud Storage
    /// </summary>
    Task<bool> ExistsAsync(string fileName, CancellationToken ct = default);
}
