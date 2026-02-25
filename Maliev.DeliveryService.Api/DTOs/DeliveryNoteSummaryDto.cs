namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>
/// Summary data for a delivery note, used in lists.
/// </summary>
public class DeliveryNoteSummaryDto
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
    /// Gets or sets the current status.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of items in the delivery note.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the name of the carrier.
    /// </summary>
    public string? CarrierName { get; set; }

    /// <summary>
    /// Gets or sets the tracking number.
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the delivery note was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
