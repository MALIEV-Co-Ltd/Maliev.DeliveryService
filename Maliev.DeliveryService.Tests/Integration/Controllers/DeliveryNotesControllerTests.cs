using System.Net;
using System.Net.Http.Json;
using Maliev.DeliveryService.Api.DTOs;
using Maliev.DeliveryService.Tests.Integration.TestFixtures;
using Xunit;

namespace Maliev.DeliveryService.Tests.Integration.Controllers;

[Collection("IntegrationTests")]
[Trait("Category", "Integration")]
public class DeliveryNotesControllerTests
{
    private readonly DeliveryServiceTestFixture _fixture;
    private readonly HttpClient _client;

    public DeliveryNotesControllerTests(DeliveryServiceTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task CreateDeliveryNote_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-INT-001",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Integration Test Customer",
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "INT-PROD-001",
                    ProductName = "Int Product",
                    QuantityOrdered = 10,
                    QuantityManufactured = 10,
                    QuantityDelivered = 10,
                    UnitOfMeasure = "pcs"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/delivery/v1/delivery-notes", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DeliveryNoteResponse>();
        Assert.NotNull(result);
        Assert.StartsWith("DN-", result.DeliveryNoteId);
    }

    [Fact]
    public async Task GetDeliveryNote_ExistingId_ReturnsOk()
    {
        // Arrange - Create one first
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-INT-002",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Integration Test Customer 2",
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest> { new() { ProductCode = "P1", QuantityOrdered = 1, QuantityManufactured = 1, QuantityDelivered = 1, UnitOfMeasure = "pcs" } }
        };
        var createResponse = await _client.PostAsJsonAsync("/delivery/v1/delivery-notes", request);
        var created = await createResponse.Content.ReadFromJsonAsync<DeliveryNoteResponse>();

        // Act
        var response = await _client.GetAsync($"/delivery/v1/delivery-notes/{created!.DeliveryNoteId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DeliveryNoteResponse>();
        Assert.Equal(created.DeliveryNoteId, result!.DeliveryNoteId);
    }
}
