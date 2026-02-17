namespace Maliev.DeliveryService.Api.DTOs;

public class PdfGenerationResponse
{
    public string DeliveryNoteId { get; set; } = null!;
    public string Status { get; set; } = "Requested";
    public string Message { get; set; } = "PDF generation request has been queued. Check back later for the download URL.";
}
