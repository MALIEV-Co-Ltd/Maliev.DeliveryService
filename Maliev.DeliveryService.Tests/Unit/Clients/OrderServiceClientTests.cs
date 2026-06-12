using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Infrastructure.HttpClients;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Clients;

public class OrderServiceClientTests
{
    private readonly Mock<ILogger<OrderServiceClient>> _mockLogger;

    public OrderServiceClientTests()
    {
        _mockLogger = new Mock<ILogger<OrderServiceClient>>();
    }

    [Fact]
    public async Task GetOrderAsync_Found_ReturnsDto()
    {
        // Arrange
        var orderId = "ORD-123";
        var billingAddressId = Guid.NewGuid();
        var shippingAddressId = Guid.NewGuid();
        var responseData = new
        {
            OrderId = orderId,
            CustomerId = Guid.NewGuid().ToString(),
            CustomerPoNumber = "PO-456",
            ServiceCategoryName = "Cat",
            ProcessTypeName = "Proc",
            OrderedQuantity = 10,
            ManufacturedQuantity = 10,
            BillingAddressId = billingAddressId,
            ShippingAddressId = shippingAddressId,
            ShippingAddressLine1 = "88 Rama IX Road",
            ShippingAddressLine2 = "Floor 12",
            ShippingCity = "Bangkok",
            ShippingProvince = "Bangkok",
            ShippingPostalCode = "10310",
            ShippingCountry = "TH",
            DeliveryContactName = "Natt Customer",
            DeliveryContactPhone = "+66810000002",
            DeliveryContactEmail = "shipping@example.test"
        };

        var httpClient = CreateHttpClient(HttpStatusCode.OK, responseData);
        var client = new OrderServiceClient(httpClient, _mockLogger.Object);

        // Act
        var result = await client.GetOrderAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.OrderId);
        Assert.Equal("PO-456", result.OrderNumber);
        Assert.Equal(billingAddressId, result.BillingAddressId);
        Assert.Equal(shippingAddressId, result.ShippingAddressId);
        Assert.Equal("88 Rama IX Road", result.ShippingAddressLine1);
        Assert.Equal("Floor 12", result.ShippingAddressLine2);
        Assert.Equal("Bangkok", result.ShippingCity);
        Assert.Equal("Bangkok", result.ShippingProvince);
        Assert.Equal("10310", result.ShippingPostalCode);
        Assert.Equal("TH", result.ShippingCountry);
        Assert.Equal("Natt Customer", result.DeliveryContactName);
        Assert.Equal("+66810000002", result.DeliveryContactPhone);
        Assert.Equal("shipping@example.test", result.DeliveryContactEmail);
        Assert.Single(result.Items);
        Assert.Equal("Cat", result.Items[0].ProductCode);
    }

    [Fact]
    public async Task GetOrderAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var httpClient = CreateHttpClient(HttpStatusCode.NotFound, null);
        var client = new OrderServiceClient(httpClient, _mockLogger.Object);

        // Act
        var result = await client.GetOrderAsync("ORD-MISSING");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrderAsync_HttpRequestException_Throws()
    {
        // Arrange
        var handler = new MockErrorHttpMessageHandler();
        var client = new OrderServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetOrderAsync("ORD-ERR"));
    }

    private class MockErrorHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Network error");
        }
    }

    private HttpClient CreateHttpClient(HttpStatusCode statusCode, object? responseData)
    {
        var handler = new MockHttpMessageHandler(statusCode, responseData);
        var client = new HttpClient(handler);
        client.BaseAddress = new Uri("http://localhost");
        return client;
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly object? _responseData;

        public MockHttpMessageHandler(HttpStatusCode statusCode, object? responseData)
        {
            _statusCode = statusCode;
            _responseData = responseData;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            if (_responseData != null)
            {
                response.Content = new StringContent(JsonSerializer.Serialize(_responseData));
            }
            else if (_statusCode == HttpStatusCode.OK)
            {
                response.Content = new StringContent("{}"); // Empty valid JSON if OK and no data
            }
            return Task.FromResult(response);
        }
    }
}
