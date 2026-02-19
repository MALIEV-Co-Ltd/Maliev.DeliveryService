namespace Maliev.DeliveryService.Api.DTOs;

public class DeliveryNoteFileResponse
{
    public Guid FileId { get; set; }
    public string DeliveryNoteId { get; set; } = null!;
    public string FileType { get; set; } = null!;
    public string OriginalFileName { get; set; } = null!;
    public string StorageUrl { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = null!;
}
