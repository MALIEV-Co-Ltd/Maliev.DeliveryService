using Maliev.DeliveryService.Application.DTOs;

namespace Maliev.DeliveryService.Application.Abstractions;

public interface IOrderServiceClient
{
    Task<OrderDetailsDto?> GetOrderAsync(string orderId, CancellationToken ct = default);
}
