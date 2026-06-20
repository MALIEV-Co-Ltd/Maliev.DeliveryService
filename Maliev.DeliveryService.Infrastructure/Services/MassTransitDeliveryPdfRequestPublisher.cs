using Maliev.DeliveryService.Application.Abstractions;
using Maliev.MessagingContracts.Contracts.Delivery;
using MassTransit;

namespace Maliev.DeliveryService.Infrastructure.Services;

/// <summary>
/// Publishes delivery-note PDF requests directly to the message bus.
/// </summary>
public sealed class MassTransitDeliveryPdfRequestPublisher : IDeliveryPdfRequestPublisher
{
    private readonly IBus _bus;

    /// <summary>
    /// Initializes a new instance of the <see cref="MassTransitDeliveryPdfRequestPublisher"/> class.
    /// </summary>
    public MassTransitDeliveryPdfRequestPublisher(IBus bus)
    {
        _bus = bus;
    }

    /// <inheritdoc />
    public Task PublishAsync(DeliveryNotePdfRequestedEvent message, CancellationToken ct = default)
    {
        return _bus.Publish(message, ct);
    }
}
