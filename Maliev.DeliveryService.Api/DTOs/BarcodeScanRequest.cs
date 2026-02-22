using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Api.DTOs;

public class BarcodeScanRequest
{
    [Required]
    public string BarcodeValue { get; set; } = string.Empty;
}
