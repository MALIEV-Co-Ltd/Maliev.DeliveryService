using Maliev.DeliveryService.Api.DTOs;

namespace Maliev.DeliveryService.Api.Clients;

/// <summary>Initializes or represents a public member.</summary>
/// <summary>Initializes or represents a public member.</summary>
public interface IOrderServiceClient
{
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    Task<OrderDetailsDto?> GetOrderAsync(string orderId, CancellationToken ct = default);
}
