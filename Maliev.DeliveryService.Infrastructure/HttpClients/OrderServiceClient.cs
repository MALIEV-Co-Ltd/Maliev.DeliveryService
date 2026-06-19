using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

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

            var orderItems = await GetOrderItemsAsync(orderId, orderData, ct);

            // Map OrderService response to DeliveryService DTO
            var dto = new OrderDetailsDto
            {
                OrderId = orderData.OrderId,
                OrderNumber = orderData.CustomerPoNumber ?? orderData.OrderId,
                Version = orderData.Version,
                CustomerId = Guid.TryParse(orderData.CustomerId, out var cid) ? cid : Guid.Empty,
                CustomerName = ResolveCustomerName(orderData),
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
                Items = orderItems
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

    /// <inheritdoc />
    public async Task<bool> SyncDeliverySnapshotAsync(
        string orderId,
        DateTime? promisedDeliveryDate,
        DateTime? actualDeliveryDate,
        string? deliveryContactName,
        string? deliveryContactPhone,
        string? deliveryContactEmail,
        CancellationToken ct = default)
    {
        try
        {
            var order = await GetOrderAsync(orderId, ct);
            if (order is null || string.IsNullOrWhiteSpace(order.Version))
            {
                _logger.LogWarning(
                    "Cannot sync delivery snapshot for order {OrderId}: OrderService returned no order/version.",
                    orderId);
                return false;
            }

            var targetOrderId = string.IsNullOrWhiteSpace(order.OrderId) ? orderId : order.OrderId;
            var response = await _httpClient.PutAsJsonAsync(
                $"order/v1/orders/{Uri.EscapeDataString(targetOrderId)}",
                new
                {
                    version = order.Version,
                    promisedDeliveryDate,
                    actualDeliveryDate,
                    deliveryContactName,
                    deliveryContactPhone,
                    deliveryContactEmail
                },
                ct);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "OrderService delivery snapshot sync returned {StatusCode} for order {OrderId}: {Body}",
                response.StatusCode,
                targetOrderId,
                body);
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Delivery snapshot sync failed for order {OrderId}.", orderId);
            return false;
        }
    }

    private async Task<List<OrderLineItemDto>> GetOrderItemsAsync(
        string orderId,
        OrderServiceResponse orderData,
        CancellationToken ct)
    {
        var response = await _httpClient.GetAsync($"order/v1/orders/{Uri.EscapeDataString(orderId)}/items", ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "OrderService item endpoint returned {StatusCode} for order {OrderId}; falling back to order summary item.",
                response.StatusCode,
                orderId);
            return [BuildFallbackItem(orderData)];
        }

        try
        {
            var items = await response.Content.ReadFromJsonAsync<List<OrderServiceItemResponse>>(_jsonOptions, ct);
            if (items is null || items.Count == 0)
            {
                return [BuildFallbackItem(orderData)];
            }

            return [.. items.Select(MapItem)];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "OrderService item endpoint returned an invalid payload for order {OrderId}; falling back to order summary item.",
                orderId);
            return [BuildFallbackItem(orderData)];
        }
    }

    private static OrderLineItemDto BuildFallbackItem(OrderServiceResponse orderData)
    {
        return new OrderLineItemDto
        {
            ProductCode = orderData.ServiceCategoryName ?? "UNK",
            ProductName = orderData.ProcessTypeName ?? "Custom Part",
            QuantityOrdered = orderData.OrderedQuantity ?? 1,
            QuantityManufactured = orderData.ManufacturedQuantity ?? orderData.OrderedQuantity ?? 1,
            UnitOfMeasure = "EA"
        };
    }

    private static string ResolveCustomerName(OrderServiceResponse orderData)
    {
        if (!string.IsNullOrWhiteSpace(orderData.BillingCompanyName))
        {
            return orderData.BillingCompanyName;
        }

        if (!string.IsNullOrWhiteSpace(orderData.DeliveryContactName))
        {
            return orderData.DeliveryContactName;
        }

        return "Unknown";
    }

    private static OrderLineItemDto MapItem(OrderServiceItemResponse item)
    {
        return new OrderLineItemDto
        {
            ProductCode = item.SourceProjectPartId?.ToString("N") ?? item.OrderItemId.ToString("N"),
            ProductName = ResolveProductName(item),
            QuantityOrdered = Math.Max(1, item.Quantity),
            QuantityManufactured = Math.Max(1, item.Quantity),
            UnitOfMeasure = "pcs"
        };
    }

    private static string ResolveProductName(OrderServiceItemResponse item)
    {
        if (!string.IsNullOrWhiteSpace(item.ConfigurationSnapshotJson))
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(item.ConfigurationSnapshotJson);
                if (document.RootElement.TryGetProperty("fileName", out JsonElement fileName) &&
                    fileName.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(fileName.GetString()))
                {
                    return fileName.GetString()!;
                }
            }
            catch (JsonException)
            {
                // Fall through to technology-based label.
            }
        }

        return string.IsNullOrWhiteSpace(item.Technology) ? "Custom Part" : $"{item.Technology} part";
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
        public string? BillingCompanyName { get; set; }
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
        public string? Version { get; set; }
    }

    private class OrderServiceItemResponse
    {
        public Guid OrderItemId { get; set; }
        public Guid? SourceProjectPartId { get; set; }
        public string? ConfigurationSnapshotJson { get; set; }
        public string? Technology { get; set; }
        public int Quantity { get; set; }
    }
}
