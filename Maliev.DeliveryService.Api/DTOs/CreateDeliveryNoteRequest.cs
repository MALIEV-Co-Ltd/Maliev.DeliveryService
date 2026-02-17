using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Api.DTOs;

public class CreateDeliveryNoteRequest
{
    public string? OrderId { get; set; }
    public int? PurchaseOrderId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    public string? CustomerName { get; set; }

    [Required]
    public DateTime DeliveryDate { get; set; }

    public string? DeliveryContactName { get; set; }
    public string? DeliveryContactPhone { get; set; }
    public string? DeliveryContactEmail { get; set; }

    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal? ShippingCost { get; set; }
    public string? ShippingCostCurrency { get; set; }

    public string? DeliveryInstructions { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<CreateDeliveryNoteItemRequest> Items { get; set; } = new();

    // Address fields (will be snapshot)
    public string? ShippingAddressLine1 { get; set; }
    public string? ShippingAddressLine2 { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingProvince { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }
}
