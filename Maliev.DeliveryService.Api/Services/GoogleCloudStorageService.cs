using Google.Cloud.Storage.V1;
using System.Net.Http;

namespace Maliev.DeliveryService.Api.Services;

/// <summary>Initializes or represents a public member.</summary>
/// <summary>Initializes or represents a public member.</summary>
public class GoogleCloudStorageService : IFileStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly ILogger<GoogleCloudStorageService> _logger;

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public GoogleCloudStorageService(
        StorageClient storageClient,
        IConfiguration configuration,
        ILogger<GoogleCloudStorageService> logger)
    {
        _storageClient = storageClient;
        _bucketName = configuration["GoogleCloudStorage:BucketName"]
            ?? throw new InvalidOperationException("GoogleCloudStorage:BucketName not configured");
        _logger = logger;
    }

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        try
        {
            // Generate unique file name with timestamp to avoid collisions
            var uniqueFileName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}/{fileName}";

            _logger.LogInformation("Uploading file to GCS: {FileName} as {UniqueFileName}", fileName, uniqueFileName);

            var obj = await _storageClient.UploadObjectAsync(
                bucket: _bucketName,
                objectName: uniqueFileName,
                contentType: contentType,
                source: fileStream,
                options: new UploadObjectOptions { PredefinedAcl = PredefinedObjectAcl.Private },
                cancellationToken: ct);

            var storageUrl = $"gs://{_bucketName}/{uniqueFileName}";

            _logger.LogInformation("File uploaded successfully: {StorageUrl}", storageUrl);

            return storageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} to GCS", fileName);
            throw;
        }
    }

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public Task<string> GetSignedUrlAsync(
        string fileName,
        TimeSpan expiration,
        CancellationToken ct = default)
    {
        try
        {
            // Extract object name from gs:// URL if needed
            var objectName = fileName.StartsWith("gs://")
                ? fileName.Substring($"gs://{_bucketName}/".Length)
                : fileName;

            // Create signed URL using UrlSigner (synchronous operation)
            var signedUrl = UrlSigner.FromCredential(
                Google.Apis.Auth.OAuth2.GoogleCredential.GetApplicationDefault().UnderlyingCredential as Google.Apis.Auth.OAuth2.ServiceAccountCredential)
                .Sign(_bucketName, objectName, expiration, HttpMethod.Get);

            return Task.FromResult(signedUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate signed URL for {FileName}", fileName);
            throw;
        }
    }

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public async Task DeleteAsync(string fileName, CancellationToken ct = default)
    {
        try
        {
            // Extract object name from gs:// URL if needed
            var objectName = fileName.StartsWith("gs://")
                ? fileName.Substring($"gs://{_bucketName}/".Length)
                : fileName;

            await _storageClient.DeleteObjectAsync(_bucketName, objectName, cancellationToken: ct);

            _logger.LogInformation("File deleted from GCS: {FileName}", fileName);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File not found in GCS (already deleted or never existed): {FileName}", fileName);
            // Not throwing - treat as success if file doesn't exist
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileName} from GCS", fileName);
            throw;
        }
    }

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public async Task<bool> ExistsAsync(string fileName, CancellationToken ct = default)
    {
        try
        {
            // Extract object name from gs:// URL if needed
            var objectName = fileName.StartsWith("gs://")
                ? fileName.Substring($"gs://{_bucketName}/".Length)
                : fileName;

            var obj = await _storageClient.GetObjectAsync(_bucketName, objectName, cancellationToken: ct);
            return obj != null;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of file {FileName} in GCS", fileName);
            throw;
        }
    }
}
