namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>Initializes or represents a public member.</summary>
/// <summary>Initializes or represents a public member.</summary>
public class DeliveryNoteFileResponse
{
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public Guid FileId { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string DeliveryNoteId { get; set; } = null!;
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string FileType { get; set; } = null!;
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string OriginalFileName { get; set; } = null!;
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string StorageUrl { get; set; } = null!;
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public long FileSizeBytes { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string? Description { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public DateTime UploadedAt { get; set; }
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public string UploadedBy { get; set; } = null!;
}
