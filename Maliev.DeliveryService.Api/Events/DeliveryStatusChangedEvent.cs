namespace Maliev.DeliveryService.Api.Events;

public sealed record DeliveryStatusChangedEvent
{
    public string DeliveryNoteId { get; init; } = null!;
    public string? OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerPhone { get; init; }
    public string PreviousStatus { get; init; } = null!;
    public string NewStatus { get; init; } = null!;
    public DateTime? ActualDeliveryTime { get; init; }
    public string? ReceivedByName { get; init; }
    public string? TrackingNumber { get; init; }
    public string? CarrierName { get; init; }
    public DateTime ChangedAt { get; init; }
    public string ChangedBy { get; init; } = null!;
}
