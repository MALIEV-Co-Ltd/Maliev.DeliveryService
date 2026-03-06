using Maliev.DeliveryService.Application.DTOs;
using Maliev.DeliveryService.Domain.Entities;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.DTOs;

public class DtoMappingExtensionsTests
{
    [Fact]
    public void ToResponse_DeliveryNote_ReturnsCorrectResponse()
    {
        var entity = new DeliveryNote
        {
            DeliveryNoteId = "DN-2026-000001",
            OrderId = "ORD-001",
            PurchaseOrderId = 12345,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            Status = DeliveryStatus.Pending,
            ShippingAddressLine1 = "123 Main St",
            ShippingCity = "Bangkok",
            ShippingProvince = "Bangkok",
            ShippingPostalCode = "10100",
            ShippingCountry = "Thailand",
            DeliveryContactName = "John Doe",
            DeliveryContactPhone = "+66-123456789",
            DeliveryContactEmail = "john@test.com",
            CarrierName = "Flash Express",
            TrackingNumber = "TRACK123",
            ShippingCost = 50.00m,
            ShippingCostCurrency = "THB",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            Version = 5,
            Items = new List<DeliveryNoteItem>
            {
                new DeliveryNoteItem
                {
                    Id = 1,
                    OrderId = "ORD-001",
                    ProductCode = "PROD-001",
                    ProductName = "Test Product",
                    QuantityOrdered = 100,
                    QuantityManufactured = 100,
                    QuantityDelivered = 50,
                    UnitOfMeasure = "pcs"
                }
            }
        };

        var result = entity.ToResponse();

        Assert.Equal(entity.DeliveryNoteId, result.DeliveryNoteId);
        Assert.Equal(entity.OrderId, result.OrderId);
        Assert.Equal(entity.PurchaseOrderId, result.PurchaseOrderId);
        Assert.Equal(entity.CustomerId, result.CustomerId);
        Assert.Equal(entity.CustomerName, result.CustomerName);
        Assert.Equal(entity.DeliveryDate, result.DeliveryDate);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(entity.ShippingAddressLine1, result.ShippingAddressLine1);
        Assert.Equal(entity.ShippingCity, result.ShippingCity);
        Assert.Equal(entity.CarrierName, result.CarrierName);
        Assert.Equal(entity.TrackingNumber, result.TrackingNumber);
        Assert.Equal(entity.ShippingCost, result.ShippingCost);
        Assert.Single(result.Items);
        Assert.Equal("PROD-001", result.Items[0].ProductCode);
        Assert.Equal(5u, result.Version);
    }

    [Fact]
    public void ToResponse_DeliveryNoteItem_ReturnsCorrectResponse()
    {
        var entity = new DeliveryNoteItem
        {
            Id = 1,
            OrderId = "ORD-001",
            PurchaseOrderItemId = 1,
            ProductCode = "PROD-001",
            ProductName = "Test Product",
            ProductDescription = "A test product",
            QuantityOrdered = 100,
            QuantityManufactured = 80,
            QuantityDelivered = 50,
            UnitOfMeasure = "pcs",
            ItemNotes = "Test notes",
            Version = 3
        };

        var result = entity.ToResponse();

        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.OrderId, result.OrderId);
        Assert.Equal(entity.PurchaseOrderItemId, result.PurchaseOrderItemId);
        Assert.Equal(entity.ProductCode, result.ProductCode);
        Assert.Equal(entity.ProductName, result.ProductName);
        Assert.Equal(entity.QuantityOrdered, result.QuantityOrdered);
        Assert.Equal(entity.QuantityDelivered, result.QuantityDelivered);
        Assert.Equal(3u, result.Version);
    }

    [Fact]
    public void ToEntity_CreateDeliveryNoteRequest_ReturnsCorrectEntity()
    {
        var request = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-001",
            PurchaseOrderId = 12345,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            DeliveryDate = DateTime.UtcNow.AddDays(1),
            ShippingAddressLine1 = "123 Main St",
            ShippingCity = "Bangkok",
            ShippingProvince = "Bangkok",
            ShippingPostalCode = "10100",
            ShippingCountry = "Thailand",
            DeliveryContactName = "John Doe",
            DeliveryContactPhone = "+66-123456789",
            DeliveryContactEmail = "john@test.com",
            CarrierName = "Flash Express",
            TrackingNumber = "TRACK123",
            ShippingCost = 50.00m,
            ShippingCostCurrency = "THB",
            DeliveryInstructions = "Handle with care",
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new CreateDeliveryNoteItemRequest
                {
                    OrderId = "ORD-001",
                    ProductCode = "PROD-001",
                    ProductName = "Test Product",
                    QuantityOrdered = 100,
                    QuantityManufactured = 80,
                    QuantityDelivered = 50,
                    UnitOfMeasure = "pcs"
                }
            }
        };

        var result = request.ToEntity("DN-2026-000001", "test-user");

        Assert.Equal("DN-2026-000001", result.DeliveryNoteId);
        Assert.Equal(request.OrderId, result.OrderId);
        Assert.Equal(DeliveryStatus.Pending, result.Status);
        Assert.Equal("123 Main St", result.ShippingAddressLine1);
        Assert.Equal("John Doe", result.DeliveryContactName);
        Assert.Equal("Flash Express", result.CarrierName);
        Assert.Single(result.Items);
        Assert.Equal("test-user", result.CreatedBy);
    }

    [Fact]
    public void ToEntity_CreateDeliveryNoteItemRequest_ReturnsCorrectEntity()
    {
        var request = new CreateDeliveryNoteItemRequest
        {
            OrderId = "ORD-001",
            PurchaseOrderItemId = 1,
            ProductCode = "PROD-001",
            ProductName = "Test Product",
            ProductDescription = "A test product",
            QuantityOrdered = 100,
            QuantityManufactured = 80,
            QuantityDelivered = 50,
            UnitOfMeasure = "pcs",
            ItemNotes = "Test notes"
        };

        var result = request.ToEntity();

        Assert.Equal(request.OrderId, result.OrderId);
        Assert.Equal(request.ProductCode, result.ProductCode);
        Assert.Equal(request.ProductName, result.ProductName);
        Assert.Equal(request.QuantityOrdered, result.QuantityOrdered);
        Assert.Equal(request.QuantityDelivered, result.QuantityDelivered);
    }

    [Fact]
    public void ToResponse_DeliveryNoteFile_ReturnsCorrectResponse()
    {
        var entity = new DeliveryNoteFile
        {
            Id = Guid.NewGuid(),
            DeliveryNoteId = "DN-2026-000001",
            FileType = FileType.Photo,
            FileName = "photo.jpg",
            StorageUrl = "gs://bucket/photo.jpg",
            ContentType = "image/jpeg",
            FileSize = 1024,
            Description = "Test photo",
            UploadedAt = DateTime.UtcNow,
            UploadedBy = "test-user",
            Version = 7
        };

        var result = entity.ToResponse();

        Assert.Equal(entity.Id, result.FileId);
        Assert.Equal(entity.DeliveryNoteId, result.DeliveryNoteId);
        Assert.Equal("Photo", result.FileType);
        Assert.Equal(entity.FileName, result.OriginalFileName);
        Assert.Equal(entity.StorageUrl, result.StorageUrl);
        Assert.Equal(entity.FileSize, result.FileSizeBytes);
        Assert.Equal(7u, result.Version);
    }
}
