namespace Maliev.DeliveryService.Application.Abstractions;

/// <summary>
/// Represents metadata and content of an uploaded file.
/// </summary>
public interface IFileData
{
    /// <summary>
    /// Opens a readable stream to the file content.
    /// </summary>
    Stream OpenReadStream();
    
    /// <summary>
    /// Gets the original file name.
    /// </summary>
    string FileName { get; }
    
    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    long Length { get; }
    
    /// <summary>
    /// Gets the MIME content type of the file.
    /// </summary>
    string ContentType { get; }
}
