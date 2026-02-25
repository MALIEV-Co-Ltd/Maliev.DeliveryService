namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>
/// Response data for a delivery note.
/// </summary>
public class DeliveryNoteResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the delivery note.
    /// </summary>
    public string DeliveryNoteId { get; set; } = null!;

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
    /// Gets or sets the actual delivery time.
    /// </summary>
    public DateTime? ActualDeliveryTime { get; set; }

    /// <summary>
    /// Gets or sets the current status.
    /// </summary>
    public string Status { get; set; } = null!;

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
    /// Gets or sets the received by name.
    /// </summary>
    public string? ReceivedByName { get; set; }

    /// <summary>
    /// Gets or sets the signed at date and time.
    /// </summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// Gets or sets internal notes.
    /// </summary>
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Gets or sets delivery instructions.
    /// </summary>
    public string? DeliveryInstructions { get; set; }

    /// <summary>
    /// Gets or sets the created at date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the created by user.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the updated at date and time.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated by user.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the row version for optimistic concurrency.
    /// </summary>
    public int RowVersion { get; set; }

    /// <summary>
    /// Gets or sets the list of items.
    /// </summary>
    public List<DeliveryNoteItemResponse> Items { get; set; } = new();
}
