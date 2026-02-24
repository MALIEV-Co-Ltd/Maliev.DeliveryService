using System.ComponentModel.DataAnnotations;

namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>Request payload for scanning a barcode on a delivery note.</summary>
public class BarcodeScanRequest
{
    /// <summary>The barcode value scanned from the package label. Max 100 characters.</summary>
    [Required]
    public string BarcodeValue { get; set; } = string.Empty;
}
