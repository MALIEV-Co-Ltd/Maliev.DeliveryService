namespace Maliev.DeliveryService.Domain.Entities;

/// <summary>
/// Represents a delivery note document.
/// </summary>
public class DeliveryNote
{
    /// <summary>
    /// Gets or sets the unique identifier for the delivery note (e.g., DN-2026-000001).
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
    /// Gets or sets the actual time when delivery was completed.
    /// </summary>
    public DateTime? ActualDeliveryTime { get; set; }

    /// <summary>
    /// Gets or sets the current status of the delivery.
    /// </summary>
    public DeliveryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the shipping address reference ID.
    /// </summary>
    public Guid? ShippingAddressId { get; set; }

    /// <summary>
    /// Gets or sets the first line of the shipping address (snapshot).
    /// </summary>
    public string? ShippingAddressLine1 { get; set; }

    /// <summary>
    /// Gets or sets the second line of the shipping address (snapshot).
    /// </summary>
    public string? ShippingAddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the shipping city (snapshot).
    /// </summary>
    public string? ShippingCity { get; set; }

    /// <summary>
    /// Gets or sets the shipping province (snapshot).
    /// </summary>
    public string? ShippingProvince { get; set; }

    /// <summary>
    /// Gets or sets the shipping postal code (snapshot).
    /// </summary>
    public string? ShippingPostalCode { get; set; }

    /// <summary>
    /// Gets or sets the shipping country (snapshot).
    /// </summary>
    public string? ShippingCountry { get; set; }

    /// <summary>
    /// Gets or sets the delivery contact name.
    /// </summary>
    public string? DeliveryContactName { get; set; }

    /// <summary>
    /// Gets or sets the delivery contact phone number.
    /// </summary>
    public string? DeliveryContactPhone { get; set; }

    /// <summary>
    /// Gets or sets the delivery contact email address.
    /// </summary>
    public string? DeliveryContactEmail { get; set; }

    /// <summary>
    /// Gets or sets the name of the shipping carrier.
    /// </summary>
    public string? CarrierName { get; set; }

    /// <summary>
    /// Gets or sets the carrier tracking number.
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the shipping cost.
    /// </summary>
    public decimal? ShippingCost { get; set; }

    /// <summary>
    /// Gets or sets the currency for the shipping cost.
    /// </summary>
    public string? ShippingCostCurrency { get; set; }

    /// <summary>
    /// Gets or sets the name of the person who received the delivery.
    /// </summary>
    public string? ReceivedByName { get; set; }

    /// <summary>
    /// Gets or sets the signature file identifier.
    /// </summary>
    public Guid? SignatureFileId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the delivery was signed.
    /// </summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// Gets or sets internal notes for the delivery.
    /// </summary>
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Gets or sets specific delivery instructions.
    /// </summary>
    public string? DeliveryInstructions { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the delivery note was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user or system that created the delivery note.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the delivery note was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user or system that last updated the delivery note.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the concurrency token for optimistic locking.
    /// </summary>
    public int RowVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the delivery note is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the delivery note was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the user or system that deleted the delivery note.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets the list of items included in this delivery note.
    /// </summary>
    public List<DeliveryNoteItem> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of files attached to this delivery note.
    /// </summary>
    public List<DeliveryNoteFile> Files { get; set; } = new();
}
