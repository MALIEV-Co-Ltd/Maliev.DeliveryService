namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>
/// Request to update delivery note details.
/// </summary>
public class UpdateDeliveryNoteRequest
{
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
    /// Gets or sets the shipping cost currency.
    /// </summary>
    public string? ShippingCostCurrency { get; set; }

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
    /// Gets or sets the delivery instructions.
    /// </summary>
    public string? DeliveryInstructions { get; set; }

    /// <summary>
    /// Gets or sets internal notes.
    /// </summary>
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Gets or sets the row version for optimistic concurrency.
    /// </summary>
    public int RowVersion { get; set; }
}
