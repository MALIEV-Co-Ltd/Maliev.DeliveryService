using Maliev.DeliveryService.Application.DTOs;

namespace Maliev.DeliveryService.Application.Abstractions;

/// <summary>
/// Client for communicating with the Order Service.
/// </summary>
public interface IOrderServiceClient
{
    /// <summary>
    /// Gets order details by order ID.
    /// </summary>
    Task<OrderDetailsDto?> GetOrderAsync(string orderId, CancellationToken ct = default);
}
