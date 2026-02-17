namespace Maliev.DeliveryService.Api.Events;

public sealed record DeliveryNotePdfRequestedEvent
{
    public string DeliveryNoteId { get; init; } = null!;
    public string RequestedBy { get; init; } = null!;
    public DateTime RequestedAt { get; init; }
}
