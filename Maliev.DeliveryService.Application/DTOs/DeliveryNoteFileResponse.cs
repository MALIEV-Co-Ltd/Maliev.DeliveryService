namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Response data for a delivery note file attachment.
/// </summary>
public class DeliveryNoteFileResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the file.
    /// </summary>
    public Guid FileId { get; set; }

    /// <summary>
    /// Gets or sets the associated delivery note ID.
    /// </summary>
    public string DeliveryNoteId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of the file.
    /// </summary>
    public string FileType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string OriginalFileName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the storage URL.
    /// </summary>
    public string StorageUrl { get; set; } = null!;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets an optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the file was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who uploaded the file.
    /// </summary>
    public string UploadedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the concurrency version.
    /// </summary>
    public uint Version { get; set; }
}

/// <summary>
/// Downloaded delivery note file content.
/// </summary>
public class DeliveryNoteFileContentResponse
{
    /// <summary>
    /// Gets or sets the file identifier.
    /// </summary>
    public Guid FileId { get; set; }

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string OriginalFileName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the MIME content type.
    /// </summary>
    public string ContentType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the file content bytes.
    /// </summary>
    public byte[] Content { get; set; } = [];
}
