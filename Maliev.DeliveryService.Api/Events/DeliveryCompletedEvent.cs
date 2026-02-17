namespace Maliev.DeliveryService.Api.Events;

public sealed record DeliveryCompletedEvent
{
    public string DeliveryNoteId { get; init; } = null!;
    public string? OrderId { get; init; }
    public int? PurchaseOrderId { get; init; }
    public DateTime CompletedAt { get; init; }
    public string ReceivedByName { get; init; } = null!;
}
