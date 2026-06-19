using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;

namespace Maliev.DeliveryService.Tests.Fakes;

/// <summary>
/// Fake implementation of IOrderServiceClient for testing (no mocking libraries)
/// </summary>
public class FakeOrderServiceClient : IOrderServiceClient
{
    private readonly Dictionary<string, OrderDetailsDto> _orders = new();

    public List<SyncedDeliverySnapshot> DeliverySnapshots { get; } = new();

    public bool FailDeliverySnapshotSync { get; set; }

    public Task<OrderDetailsDto?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public Task<bool> SyncDeliverySnapshotAsync(
        string orderId,
        DateTime? promisedDeliveryDate,
        DateTime? actualDeliveryDate,
        string? deliveryContactName,
        string? deliveryContactPhone,
        string? deliveryContactEmail,
        CancellationToken ct = default)
    {
        DeliverySnapshots.Add(new SyncedDeliverySnapshot(
            orderId,
            promisedDeliveryDate,
            actualDeliveryDate,
            deliveryContactName,
            deliveryContactPhone,
            deliveryContactEmail));
        return Task.FromResult(!FailDeliverySnapshotSync);
    }

    // Test helper methods
    public void AddOrder(OrderDetailsDto order)
    {
        _orders[order.OrderId] = order;
    }

    public void Clear()
    {
        _orders.Clear();
        DeliverySnapshots.Clear();
        FailDeliverySnapshotSync = false;
    }

    public sealed record SyncedDeliverySnapshot(
        string OrderId,
        DateTime? PromisedDeliveryDate,
        DateTime? ActualDeliveryDate,
        string? DeliveryContactName,
        string? DeliveryContactPhone,
        string? DeliveryContactEmail);
}
