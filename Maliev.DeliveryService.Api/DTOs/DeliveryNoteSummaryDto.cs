namespace Maliev.DeliveryService.Api.DTOs;

public class DeliveryNoteSummaryDto
{
    public string DeliveryNoteId { get; set; } = null!;
    public string? OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string Status { get; set; } = null!;
    public int ItemCount { get; set; }
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}
