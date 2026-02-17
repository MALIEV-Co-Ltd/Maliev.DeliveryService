namespace Maliev.DeliveryService.Api.Events;

public sealed record DeliveryNoteCreatedEvent
{
    public string DeliveryNoteId { get; init; } = null!;
    public string? OrderId { get; init; }
    public int? PurchaseOrderId { get; init; }
    public Guid CustomerId { get; init; }
    public DateTime DeliveryDate { get; init; }
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = null!;
}
