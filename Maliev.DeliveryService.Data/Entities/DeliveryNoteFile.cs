namespace Maliev.DeliveryService.Data.Entities;

/// <summary>Represents a file (image, document) attached to a delivery note.</summary>
public class DeliveryNoteFile
{
    /// <summary>Unique identifier for the file record.</summary>
    public Guid Id { get; set; }
    /// <summary>Identifier of the associated delivery note.</summary>
    public string DeliveryNoteId { get; set; } = null!;
    /// <summary>Original name of the uploaded file.</summary>
    public string FileName { get; set; } = null!;
    /// <summary>Permanent storage URL of the file.</summary>
    public string StorageUrl { get; set; } = null!;
    /// <summary>MIME content type of the file.</summary>
    public string ContentType { get; set; } = null!;
    /// <summary>Size of the file in bytes.</summary>
    public long FileSize { get; set; }
    /// <summary>The functional type of the file (e.g., Signature, Photo).</summary>
    public FileType FileType { get; set; }
    /// <summary>Optional description of the file content.</summary>
    public string? Description { get; set; }
    /// <summary>Timestamp when the file was uploaded.</summary>
    public DateTime UploadedAt { get; set; }
    /// <summary>User who uploaded the file.</summary>
    public string UploadedBy { get; set; } = null!;
    /// <summary>Indicates if the file has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }
    /// <summary>Timestamp when the file was deleted.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>The delivery note this file belongs to.</summary>
    public DeliveryNote DeliveryNote { get; set; } = null!;
}
