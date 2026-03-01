using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;

namespace Maliev.DeliveryService.Tests.Fakes;

/// <summary>
/// Fake implementation of IOrderServiceClient for testing (no mocking libraries)
/// </summary>
public class FakeOrderServiceClient : IOrderServiceClient
{
    private readonly Dictionary<string, OrderDetailsDto> _orders = new();

    public Task<OrderDetailsDto?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    // Test helper methods
    public void AddOrder(OrderDetailsDto order)
    {
        _orders[order.OrderId] = order;
    }

    public void Clear()
    {
        _orders.Clear();
    }
}
