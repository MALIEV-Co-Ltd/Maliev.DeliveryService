using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>Request payload for creating a new delivery note.</summary>
public class CreateDeliveryNoteRequest
{
    /// <summary>Associated order identifier.</summary>
    public string? OrderId { get; set; }
    /// <summary>Associated purchase order identifier.</summary>
    public int? PurchaseOrderId { get; set; }

    /// <summary>Customer identifier.</summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>Customer name.</summary>
    public string? CustomerName { get; set; }

    /// <summary>Scheduled delivery date.</summary>
    [Required]
    public DateTime DeliveryDate { get; set; }

    /// <summary>Contact name for the delivery.</summary>
    public string? DeliveryContactName { get; set; }
    /// <summary>Contact phone number for the delivery.</summary>
    public string? DeliveryContactPhone { get; set; }
    /// <summary>Contact email address for the delivery.</summary>
    public string? DeliveryContactEmail { get; set; }

    /// <summary>Carrier name.</summary>
    public string? CarrierName { get; set; }
    /// <summary>Tracking number.</summary>
    public string? TrackingNumber { get; set; }
    /// <summary>Shipping cost.</summary>
    public decimal? ShippingCost { get; set; }
    /// <summary>Currency for shipping cost.</summary>
    public string? ShippingCostCurrency { get; set; }

    /// <summary>Delivery instructions for the driver.</summary>
    public string? DeliveryInstructions { get; set; }

    /// <summary>Items to be included in this delivery.</summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<CreateDeliveryNoteItemRequest> Items { get; set; } = new();

    /// <summary>Shipping address line 1.</summary>
    public string? ShippingAddressLine1 { get; set; }
    /// <summary>Shipping address line 2.</summary>
    public string? ShippingAddressLine2 { get; set; }
    /// <summary>Shipping city.</summary>
    public string? ShippingCity { get; set; }
    /// <summary>Shipping province.</summary>
    public string? ShippingProvince { get; set; }
    /// <summary>Shipping postal code.</summary>
    public string? ShippingPostalCode { get; set; }
    /// <summary>Shipping country.</summary>
    public string? ShippingCountry { get; set; }
}
