namespace Maliev.DeliveryService.Api.DTOs;

/// <summary>
/// Response data for a PDF generation request.
/// </summary>
public class PdfGenerationResponse
{
    /// <summary>
    /// Gets or sets the associated delivery note ID.
    /// </summary>
    public string DeliveryNoteId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the status of the request.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets a message describing the result.
    /// </summary>
    public string Message { get; set; } = null!;
}
