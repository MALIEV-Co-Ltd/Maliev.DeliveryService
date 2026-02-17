using Maliev.DeliveryService.Api.DTOs;

namespace Maliev.DeliveryService.Api.Clients;

public interface IOrderServiceClient
{
    Task<OrderDetailsDto?> GetOrderAsync(string orderId, CancellationToken ct = default);
}
