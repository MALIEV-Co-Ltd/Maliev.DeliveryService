using System.Net;
using System.Net.Http.Json;
using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Maliev.DeliveryService.Infrastructure.HttpClients;

/// <summary>
/// HTTP client for communicating with the Order Service.
/// </summary>
public class OrderServiceClient : IOrderServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of OrderServiceClient.
    /// </summary>
    public OrderServiceClient(HttpClient httpClient, ILogger<OrderServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OrderDetailsDto?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching order details for {OrderId} from OrderService", orderId);

            // Get order from order service
            var response = await _httpClient.GetAsync($"order/v1/orders/{orderId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order {OrderId} not found in OrderService", orderId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var orderData = await response.Content.ReadFromJsonAsync<OrderServiceResponse>(cancellationToken: ct);
            if (orderData == null)
            {
                _logger.LogWarning("Received empty or invalid response from OrderService for {OrderId}", orderId);
                return null;
            }

            // Map OrderService response to DeliveryService DTO
            var dto = new OrderDetailsDto
            {
                OrderId = orderData.OrderId,
                OrderNumber = orderData.CustomerPoNumber ?? orderData.OrderId,
                CustomerId = Guid.TryParse(orderData.CustomerId, out var cid) ? cid : Guid.Empty,
                CustomerName = "Unknown", // CustomerName is not provided by OrderService
                BillingAddressId = orderData.BillingAddressId,
                ShippingAddressId = orderData.ShippingAddressId,
                ShippingAddressLine1 = orderData.ShippingAddressLine1,
                ShippingAddressLine2 = orderData.ShippingAddressLine2,
                ShippingCity = orderData.ShippingCity,
                ShippingProvince = orderData.ShippingProvince,
                ShippingPostalCode = orderData.ShippingPostalCode,
                ShippingCountry = orderData.ShippingCountry,
                DeliveryContactName = orderData.DeliveryContactName,
                DeliveryContactPhone = orderData.DeliveryContactPhone,
                DeliveryContactEmail = orderData.DeliveryContactEmail,
                Items = new List<OrderLineItemDto>
                {
                    new OrderLineItemDto
                    {
                        ProductCode = orderData.ServiceCategoryName ?? "UNK",
                        ProductName = orderData.ProcessTypeName ?? "Custom Part",
                        QuantityOrdered = orderData.OrderedQuantity ?? 1,
                        QuantityManufactured = orderData.ManufacturedQuantity ?? orderData.OrderedQuantity ?? 1,
                        UnitOfMeasure = "EA"
                    }
                }
            };

            return dto;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch order {OrderId} from OrderService due to an HTTP error.", orderId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching order {OrderId} from OrderService.", orderId);
            throw;
        }
    }

    // Private response model representing the external OrderService payload
    private class OrderServiceResponse
    {
        public string OrderId { get; set; } = null!;
        public string CustomerId { get; set; } = null!;
        public string? CustomerPoNumber { get; set; }
        public string? ServiceCategoryName { get; set; }
        public string? ProcessTypeName { get; set; }
        public int? OrderedQuantity { get; set; }
        public int? ManufacturedQuantity { get; set; }
        public Guid? BillingAddressId { get; set; }
        public Guid? ShippingAddressId { get; set; }
        public string? ShippingAddressLine1 { get; set; }
        public string? ShippingAddressLine2 { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingProvince { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string? ShippingCountry { get; set; }
        public string? DeliveryContactName { get; set; }
        public string? DeliveryContactPhone { get; set; }
        public string? DeliveryContactEmail { get; set; }
    }
}
