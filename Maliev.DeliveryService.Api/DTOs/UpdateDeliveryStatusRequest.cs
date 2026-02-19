using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Api.DTOs;

public class UpdateDeliveryStatusRequest
{
    [Required]
    public string NewStatus { get; set; } = null!;

    public DateTime? ActualDeliveryTime { get; set; }
    public string? ReceivedByName { get; set; }
}
