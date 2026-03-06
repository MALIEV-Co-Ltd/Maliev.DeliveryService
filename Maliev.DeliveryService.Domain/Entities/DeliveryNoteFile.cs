namespace Maliev.DeliveryService.Domain.Entities;

/// <summary>
/// Represents a file attached to a delivery note.
/// </summary>
public class DeliveryNoteFile
{
    /// <summary>
    /// Gets or sets the unique identifier for the file attachment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the associated delivery note ID.
    /// </summary>
    public string DeliveryNoteId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the original name of the file.
    /// </summary>
    public string FileName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the storage URL or path for the file.
    /// </summary>
    public string StorageUrl { get; set; } = null!;

    /// <summary>
    /// Gets or sets the MIME content type of the file.
    /// </summary>
    public string ContentType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the type of the file attachment.
    /// </summary>
    public FileType FileType { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the file.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the file was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Gets or sets the user or system that uploaded the file.
    /// </summary>
    public string UploadedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the file is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the file was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the associated delivery note navigation property.
    /// </summary>
    public DeliveryNote DeliveryNote { get; set; } = null!;

    /// <summary>
    /// Gets or sets the concurrency version (maps to PostgreSQL xmin).
    /// </summary>
    public uint Version { get; set; }
}
