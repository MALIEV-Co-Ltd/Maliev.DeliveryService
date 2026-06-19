using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.DeliveryService.Api.Authorization;
using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.DeliveryService.Api.Controllers;

/// <summary>
/// Controller for shipping courier, rate, and tracking operations.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("delivery/v{version:apiVersion}/shipping")]
public class ShippingController : ControllerBase
{
    private readonly IShippingGatewayService _shippingGatewayService;
    private readonly ILogger<ShippingController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingController"/> class.
    /// </summary>
    public ShippingController(
        IShippingGatewayService shippingGatewayService,
        ILogger<ShippingController> logger)
    {
        _shippingGatewayService = shippingGatewayService;
        _logger = logger;
    }

    /// <summary>
    /// Gets available courier options.
    /// </summary>
    [HttpGet("couriers")]
    [RequirePermission(DeliveryPermissions.DeliveryNotes.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<ShippingCourierResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShippingCourierResponse>>> GetCouriers(CancellationToken ct)
    {
        var result = await _shippingGatewayService.GetCouriersAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets live shipping rates from the configured shipping gateway.
    /// </summary>
    [HttpPost("rates")]
    [RequirePermission(DeliveryPermissions.DeliveryNotes.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<ShippingRateOptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<ShippingRateOptionResponse>>> GetRates(
        [FromBody] ShippingRateRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _shippingGatewayService.GetRatesAsync(request, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid shipping rate request");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Shipping rate request could not be completed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets shipment tracking status by tracking code.
    /// </summary>
    [HttpGet("tracking/{trackingCode}")]
    [RequirePermission(DeliveryPermissions.DeliveryNotes.Read)]
    [ProducesResponseType(typeof(ShippingTrackingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ShippingTrackingResponse>> GetTracking(
        [FromRoute] string trackingCode,
        CancellationToken ct)
    {
        try
        {
            var result = await _shippingGatewayService.GetTrackingAsync(trackingCode, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid shipping tracking request");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Shipping tracking request could not be completed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }
}
