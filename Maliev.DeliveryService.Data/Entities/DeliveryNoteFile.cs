namespace Maliev.DeliveryService.Data.Entities;

public class DeliveryNoteFile
{
    public Guid Id { get; set; }
    public string DeliveryNoteId { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string StorageUrl { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSize { get; set; }
    public FileType FileType { get; set; }
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation Property
    public DeliveryNote DeliveryNote { get; set; } = null!;
}
