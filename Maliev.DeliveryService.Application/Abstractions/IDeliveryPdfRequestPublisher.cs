using Maliev.MessagingContracts.Contracts.Delivery;

namespace Maliev.DeliveryService.Application.Abstractions;

/// <summary>
/// Publishes delivery-note PDF generation requests.
/// </summary>
public interface IDeliveryPdfRequestPublisher
{
    /// <summary>
    /// Publishes a delivery-note PDF generation request.
    /// </summary>
    Task PublishAsync(DeliveryNotePdfRequestedEvent message, CancellationToken ct = default);
}
