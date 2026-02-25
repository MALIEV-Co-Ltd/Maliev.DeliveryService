namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>
/// Request to create a new delivery note.
/// </summary>
public class CreateDeliveryNoteRequest
{
    /// <summary>
    /// Gets or sets the associated order ID.
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the associated purchase order ID.
    /// </summary>
    public int? PurchaseOrderId { get; set; }

    /// <summary>
    /// Gets or sets the customer ID.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Gets or sets the scheduled delivery date.
    /// </summary>
    public DateTime DeliveryDate { get; set; }

    /// <summary>
    /// Gets or sets the shipping address line 1.
    /// </summary>
    public string? ShippingAddressLine1 { get; set; }

    /// <summary>
    /// Gets or sets the shipping address line 2.
    /// </summary>
    public string? ShippingAddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the shipping city.
    /// </summary>
    public string? ShippingCity { get; set; }

    /// <summary>
    /// Gets or sets the shipping province.
    /// </summary>
    public string? ShippingProvince { get; set; }

    /// <summary>
    /// Gets or sets the shipping postal code.
    /// </summary>
    public string? ShippingPostalCode { get; set; }

    /// <summary>
    /// Gets or sets the shipping country.
    /// </summary>
    public string? ShippingCountry { get; set; }

    /// <summary>
    /// Gets or sets the delivery contact name.
    /// </summary>
    public string? DeliveryContactName { get; set; }

    /// <summary>
    /// Gets or sets the delivery contact phone.
    /// </summary>
    public string? DeliveryContactPhone { get; set; }

    /// <summary>
    /// Gets or sets the delivery contact email.
    /// </summary>
    public string? DeliveryContactEmail { get; set; }

    /// <summary>
    /// Gets or sets the carrier name.
    /// </summary>
    public string? CarrierName { get; set; }

    /// <summary>
    /// Gets or sets the tracking number.
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the shipping cost.
    /// </summary>
    public decimal? ShippingCost { get; set; }

    /// <summary>
    /// Gets or sets the shipping cost currency.
    /// </summary>
    public string? ShippingCostCurrency { get; set; }

    /// <summary>
    /// Gets or sets delivery instructions.
    /// </summary>
    public string? DeliveryInstructions { get; set; }

    /// <summary>
    /// Gets or sets the list of items to include in the delivery note.
    /// </summary>
    public List<CreateDeliveryNoteItemRequest> Items { get; set; } = new();
}
