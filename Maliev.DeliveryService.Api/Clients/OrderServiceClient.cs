using Maliev.DeliveryService.Api.DTOs;

namespace Maliev.DeliveryService.Api.Clients;

public class OrderServiceClient : IOrderServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderServiceClient> _logger;

    public OrderServiceClient(HttpClient httpClient, ILogger<OrderServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OrderDetailsDto?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        try
        {
            // TODO: Replace with actual OrderService endpoint when available
            // For now, return null (integration will be completed when OrderService API is ready)
            _logger.LogWarning("OrderService integration not yet implemented. OrderId: {OrderId}", orderId);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch order {OrderId} from OrderService", orderId);
            throw;
        }
    }
}
