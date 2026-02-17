using Maliev.DeliveryService.Data.Entities;

namespace Maliev.DeliveryService.Api.DTOs;

public class UploadFileRequest
{
    public IFormFile File { get; set; } = null!;
    public FileType FileType { get; set; }
    public string? Description { get; set; }
}
