using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>Request for updating the status of a delivery note.</summary>
public class UpdateDeliveryStatusRequest
{
    /// <summary>The new status to apply.</summary>
    [Required]
    public string NewStatus { get; set; } = null!;

    /// <summary>The actual time of delivery.</summary>
    public DateTime? ActualDeliveryTime { get; set; }
    /// <summary>The name of the person who received the delivery.</summary>
    public string? ReceivedByName { get; set; }
}
