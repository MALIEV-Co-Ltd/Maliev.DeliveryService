namespace Maliev.DeliveryService.Api.Events;

/// <summary>
/// Event consumed from OrderService when an order is completed
/// </summary>
public sealed record OrderCompletedEvent
{
    public string OrderId { get; init; } = null!;
    public Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public DateTime CompletedAt { get; init; }
    public List<OrderLineItem> Items { get; init; } = new();
}

public sealed record OrderLineItem
{
    public string ProductCode { get; init; } = null!;
    public string ProductName { get; init; } = null!;
    public decimal QuantityOrdered { get; init; }
    public decimal QuantityManufactured { get; init; }
    public string UnitOfMeasure { get; init; } = null!;
}
