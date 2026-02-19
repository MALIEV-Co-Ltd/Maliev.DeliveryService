using Maliev.DeliveryService.Data.Entities;

namespace Maliev.DeliveryService.Api.DTOs;

public static class DtoMappingExtensions
{
    public static DeliveryNoteResponse ToResponse(this DeliveryNote entity)
    {
        return new DeliveryNoteResponse
        {
            DeliveryNoteId = entity.DeliveryNoteId,
            OrderId = entity.OrderId,
            PurchaseOrderId = entity.PurchaseOrderId,
            CustomerId = entity.CustomerId,
            CustomerName = entity.CustomerName,
            DeliveryDate = entity.DeliveryDate,
            ActualDeliveryTime = entity.ActualDeliveryTime,
            Status = entity.Status.ToString(),

            ShippingAddressLine1 = entity.ShippingAddressLine1,
            ShippingAddressLine2 = entity.ShippingAddressLine2,
            ShippingCity = entity.ShippingCity,
            ShippingProvince = entity.ShippingProvince,
            ShippingPostalCode = entity.ShippingPostalCode,
            ShippingCountry = entity.ShippingCountry,

            DeliveryContactName = entity.DeliveryContactName,
            DeliveryContactPhone = entity.DeliveryContactPhone,
            DeliveryContactEmail = entity.DeliveryContactEmail,

            CarrierName = entity.CarrierName,
            TrackingNumber = entity.TrackingNumber,
            ShippingCost = entity.ShippingCost,
            ShippingCostCurrency = entity.ShippingCostCurrency,

            ReceivedByName = entity.ReceivedByName,
            SignedAt = entity.SignedAt,

            InternalNotes = entity.InternalNotes,
            DeliveryInstructions = entity.DeliveryInstructions,

            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy,

            RowVersion = entity.RowVersion,

            Items = entity.Items.Select(i => i.ToResponse()).ToList()
        };
    }

    public static DeliveryNoteItemResponse ToResponse(this DeliveryNoteItem entity)
    {
        return new DeliveryNoteItemResponse
        {
            Id = entity.Id,
            OrderId = entity.OrderId,
            PurchaseOrderItemId = entity.PurchaseOrderItemId,
            ProductCode = entity.ProductCode,
            ProductName = entity.ProductName,
            ProductDescription = entity.ProductDescription,
            QuantityOrdered = entity.QuantityOrdered,
            QuantityManufactured = entity.QuantityManufactured,
            QuantityDelivered = entity.QuantityDelivered,
            UnitOfMeasure = entity.UnitOfMeasure,
            ItemNotes = entity.ItemNotes
        };
    }

    public static DeliveryNote ToEntity(this CreateDeliveryNoteRequest request, string deliveryNoteId, string createdBy)
    {
        return new DeliveryNote
        {
            DeliveryNoteId = deliveryNoteId,
            OrderId = request.OrderId,
            PurchaseOrderId = request.PurchaseOrderId,
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            DeliveryDate = request.DeliveryDate,
            Status = DeliveryStatus.Pending,

            ShippingAddressLine1 = request.ShippingAddressLine1,
            ShippingAddressLine2 = request.ShippingAddressLine2,
            ShippingCity = request.ShippingCity,
            ShippingProvince = request.ShippingProvince,
            ShippingPostalCode = request.ShippingPostalCode,
            ShippingCountry = request.ShippingCountry,

            DeliveryContactName = request.DeliveryContactName,
            DeliveryContactPhone = request.DeliveryContactPhone,
            DeliveryContactEmail = request.DeliveryContactEmail,

            CarrierName = request.CarrierName,
            TrackingNumber = request.TrackingNumber,
            ShippingCost = request.ShippingCost,
            ShippingCostCurrency = request.ShippingCostCurrency,

            DeliveryInstructions = request.DeliveryInstructions,

            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,

            Items = request.Items.Select(i => i.ToEntity()).ToList()
        };
    }

    public static DeliveryNoteItem ToEntity(this CreateDeliveryNoteItemRequest request)
    {
        return new DeliveryNoteItem
        {
            OrderId = request.OrderId,
            PurchaseOrderItemId = request.PurchaseOrderItemId,
            ProductCode = request.ProductCode,
            ProductName = request.ProductName,
            ProductDescription = request.ProductDescription,
            QuantityOrdered = request.QuantityOrdered,
            QuantityManufactured = request.QuantityManufactured,
            QuantityDelivered = request.QuantityDelivered,
            UnitOfMeasure = request.UnitOfMeasure,
            ItemNotes = request.ItemNotes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static DeliveryNoteFileResponse ToResponse(this DeliveryNoteFile entity)
    {
        return new DeliveryNoteFileResponse
        {
            FileId = entity.Id,
            DeliveryNoteId = entity.DeliveryNoteId,
            FileType = entity.FileType.ToString(),
            OriginalFileName = entity.FileName,
            StorageUrl = entity.StorageUrl,
            FileSizeBytes = entity.FileSize,
            Description = entity.Description,
            UploadedAt = entity.UploadedAt,
            UploadedBy = entity.UploadedBy
        };
    }
}
