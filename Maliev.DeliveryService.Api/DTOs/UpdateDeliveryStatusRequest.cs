namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>
/// Request to update the status of a delivery.
/// </summary>
public class UpdateDeliveryStatusRequest
{
    /// <summary>
    /// Gets or sets the new status for the delivery.
    /// </summary>
    public string NewStatus { get; set; } = null!;

    /// <summary>
    /// Gets or sets the actual delivery time.
    /// </summary>
    public DateTime? ActualDeliveryTime { get; set; }

    /// <summary>
    /// Gets or sets the name of the person who received the delivery.
    /// </summary>
    public string? ReceivedByName { get; set; }

    /// <summary>
    /// Gets or sets the signature file ID.
    /// </summary>
    public Guid? SignatureFileId { get; set; }
}
