using System.Net;
using System.Net.Http.Json;
using Maliev.DeliveryService.Api.DTOs;
using Maliev.DeliveryService.Tests.Integration.TestFixtures;
using Xunit;

namespace Maliev.DeliveryService.Tests.Integration.Controllers;

[Trait("Category", "Integration")]
public class DeliveryNotesControllerTests : BaseIntegrationTest
{
    public DeliveryNotesControllerTests(DeliveryServiceTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateDeliveryNote_ValidRequest_ReturnsCreated()
    {
        // Arrange
        await CleanDatabaseAsync();
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = $"ORD-INT-{Guid.NewGuid():N}",
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
        var response = await Client.PostAsJsonAsync("/delivery/v1/delivery-notes", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DeliveryNoteResponse>();
        Assert.NotNull(result);
        Assert.StartsWith("DN-", result.DeliveryNoteId);
    }

    [Fact]
    public async Task SearchDeliveryNotes_ValidFilter_ReturnsOk()
    {
        // Arrange
        await CleanDatabaseAsync();
        var customerId = Guid.NewGuid();
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = $"ORD-SEARCH-{Guid.NewGuid():N}",
            CustomerId = customerId,
            CustomerName = "Search Customer",
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "P1",
                    ProductName = "Product 1",
                    QuantityOrdered = 1,
                    QuantityManufactured = 1,
                    QuantityDelivered = 1,
                    UnitOfMeasure = "pcs"
                }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/delivery/v1/delivery-notes", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Act
        var response = await Client.GetAsync($"/delivery/v1/delivery-notes?customerId={customerId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<DeliveryNoteSummaryDto>>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    [Fact]
    public async Task UpdateDeliveryStatus_ValidTransition_ReturnsOk()
    {
        // Arrange
        await CleanDatabaseAsync();
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = $"ORD-STATUS-{Guid.NewGuid():N}",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Status Customer",
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "P1",
                    ProductName = "Product 1",
                    QuantityOrdered = 1,
                    QuantityManufactured = 1,
                    QuantityDelivered = 1,
                    UnitOfMeasure = "pcs"
                }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/delivery/v1/delivery-notes", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<DeliveryNoteResponse>();
        Assert.NotNull(created);

        var updateRequest = new UpdateDeliveryStatusRequest { NewStatus = "InTransit" };

        // Act
        var response = await Client.PatchAsJsonAsync($"/delivery/v1/delivery-notes/{created.DeliveryNoteId}/status", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DeliveryNoteResponse>();
        Assert.Equal("InTransit", result!.Status);
    }

    [Fact]
    public async Task DeleteDeliveryNote_PendingStatus_ReturnsNoContent()
    {
        // Arrange
        await CleanDatabaseAsync();
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = $"ORD-DELETE-{Guid.NewGuid():N}",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Delete Customer",
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "P1",
                    ProductName = "Product 1",
                    QuantityOrdered = 1,
                    QuantityManufactured = 1,
                    QuantityDelivered = 1,
                    UnitOfMeasure = "pcs"
                }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/delivery/v1/delivery-notes", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<DeliveryNoteResponse>();
        Assert.NotNull(created);

        // Act
        var response = await Client.DeleteAsync($"/delivery/v1/delivery-notes/{created.DeliveryNoteId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetDeliveryNote_NotFound_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/delivery/v1/delivery-notes/NON-EXISTENT");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDeliveryNote_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = "", // Invalid: no OrderId or PurchaseOrderId
            Items = new List<CreateDeliveryNoteItemRequest>() // Invalid: no items
        };

        // Act
        var response = await Client.PostAsJsonAsync("/delivery/v1/delivery-notes", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDeliveryStatus_InvalidTransition_ReturnsBadRequest()
    {
        // Arrange
        await CleanDatabaseAsync();
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = $"ORD-INVALID-TRANS-{Guid.NewGuid():N}",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Status Customer",
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "P1",
                    ProductName = "Product 1",
                    QuantityOrdered = 1,
                    QuantityManufactured = 1,
                    QuantityDelivered = 1,
                    UnitOfMeasure = "pcs"
                }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/delivery/v1/delivery-notes", request);
        var created = await createResponse.Content.ReadFromJsonAsync<DeliveryNoteResponse>();

        // Act: Pending -> Delivered directly (invalid, must go through InTransit)
        var updateRequest = new UpdateDeliveryStatusRequest { NewStatus = "Delivered", ReceivedByName = "John" };
        var response = await Client.PatchAsJsonAsync($"/delivery/v1/delivery-notes/{created!.DeliveryNoteId}/status", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
