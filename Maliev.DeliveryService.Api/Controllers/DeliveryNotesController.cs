using Asp.Versioning;
using Maliev.DeliveryService.Api.DTOs;
using Maliev.DeliveryService.Api.Extensions;
using Maliev.DeliveryService.Api.Services;
using Maliev.MessagingContracts.Contracts.Delivery;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.DeliveryService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("delivery/v{version:apiVersion}/delivery-notes")]
public class DeliveryNotesController : ControllerBase
{
    private readonly IDeliveryNoteService _deliveryNoteService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeliveryNotesController> _logger;

    public DeliveryNotesController(
        IDeliveryNoteService deliveryNoteService,
        IPublishEndpoint publishEndpoint,
        ILogger<DeliveryNotesController> logger)
    {
        _deliveryNoteService = deliveryNoteService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Create a new delivery note
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DeliveryNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeliveryNoteResponse>> CreateDeliveryNote(
        [FromBody] CreateDeliveryNoteRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _deliveryNoteService.CreateAsync(request, userId, ct);

            return CreatedAtAction(
                nameof(GetDeliveryNote),
                new { id = result.DeliveryNoteId, version = "1.0" },
                result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for creating delivery note");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search and filter delivery notes with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<DeliveryNoteSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<DeliveryNoteSummaryDto>>> SearchDeliveryNotes(
        [FromQuery] DeliveryNoteFilterRequest filter,
        CancellationToken ct)
    {
        var principalId = User.GetUserId();
        var result = await _deliveryNoteService.SearchAsync(filter, principalId, ct);

        return Ok(result);
    }

    /// <summary>
    /// Get a delivery note by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DeliveryNoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryNoteResponse>> GetDeliveryNote(
        [FromRoute] string id,
        CancellationToken ct)
    {
        var result = await _deliveryNoteService.GetByIdAsync(id, ct);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Update delivery note status
    /// </summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(DeliveryNoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryNoteResponse>> UpdateDeliveryStatus(
        [FromRoute] string id,
        [FromBody] UpdateDeliveryStatusRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _deliveryNoteService.UpdateStatusAsync(id, request, userId, ct);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid status update request for delivery note {DeliveryNoteId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid status transition for delivery note {DeliveryNoteId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Request PDF generation for a delivery note
    /// </summary>
    [HttpPost("{id}/generate-pdf")]
    [ProducesResponseType(typeof(PdfGenerationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PdfGenerationResponse>> GeneratePdf(
        [FromRoute] string id,
        CancellationToken ct)
    {
        // Check if delivery note exists
        var deliveryNote = await _deliveryNoteService.GetByIdAsync(id, ct);
        if (deliveryNote == null)
        {
            return NotFound();
        }

        // Publish PDF requested event
        try
        {
            var userId = User.GetUserId();
            await _publishEndpoint.Publish(new DeliveryNotePdfRequestedEvent
            {
                DeliveryNoteId = id,
                RequestedBy = userId,
                RequestedAt = DateTime.UtcNow
            }, ct);

            _logger.LogInformation(
                "PDF generation requested for delivery note {DeliveryNoteId} by {UserId}",
                id, userId);

            return Accepted(new PdfGenerationResponse
            {
                DeliveryNoteId = id,
                Status = "Requested",
                Message = "PDF generation request has been queued. The PDF will be generated asynchronously."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish PDF generation request for delivery note {DeliveryNoteId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to queue PDF generation request" });
        }
    }

    /// <summary>
    /// Upload a file attachment to a delivery note
    /// </summary>
    [HttpPost("{id}/files")]
    [ProducesResponseType(typeof(DeliveryNoteFileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryNoteFileResponse>> UploadFile(
        [FromRoute] string id,
        [FromForm] IFormFile file,
        [FromForm] string fileType,
        [FromForm] string? description,
        CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<Data.Entities.FileType>(fileType, ignoreCase: true, out var fileTypeEnum))
            {
                return BadRequest(new { error = $"Invalid file type: {fileType}" });
            }

            var userId = User.GetUserId();
            var result = await _deliveryNoteService.AddFileAsync(id, file, fileTypeEnum, description, userId, ct);

            return CreatedAtAction(
                nameof(GetFiles),
                new { id, version = "1.0" },
                result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid file upload request for delivery note {DeliveryNoteId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to delivery note {DeliveryNoteId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to upload file. Please try again later." });
        }
    }

    /// <summary>
    /// Get all file attachments for a delivery note
    /// </summary>
    [HttpGet("{id}/files")]
    [ProducesResponseType(typeof(List<DeliveryNoteFileResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DeliveryNoteFileResponse>>> GetFiles(
        [FromRoute] string id,
        CancellationToken ct)
    {
        var files = await _deliveryNoteService.GetFilesAsync(id, ct);
        return Ok(files);
    }

    /// <summary>
    /// Update delivery note carrier, tracking, and contact information
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DeliveryNoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeliveryNoteResponse>> UpdateDeliveryNote(
        [FromRoute] string id,
        [FromBody] UpdateDeliveryNoteRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _deliveryNoteService.UpdateAsync(id, request, userId, ct);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("terminal status"))
        {
            _logger.LogWarning(ex, "Attempted to update delivery note {DeliveryNoteId} in terminal status", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another user"))
        {
            _logger.LogWarning(ex, "Concurrency conflict updating delivery note {DeliveryNoteId}", id);
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update delivery note {DeliveryNoteId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to update delivery note" });
        }
    }

    /// <summary>
    /// Soft delete a delivery note (Pending status only)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDeliveryNote(
        [FromRoute] string id,
        CancellationToken ct)
    {
        try
        {
            var userId = User.GetUserId();
            await _deliveryNoteService.SoftDeleteAsync(id, userId, ct);

            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot delete"))
        {
            _logger.LogWarning(ex, "Attempted to delete delivery note {DeliveryNoteId} in non-Pending status", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete delivery note {DeliveryNoteId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to delete delivery note" });
        }
    }
}
