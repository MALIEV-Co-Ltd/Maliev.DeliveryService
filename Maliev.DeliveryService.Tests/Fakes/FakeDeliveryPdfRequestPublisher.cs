using Maliev.DeliveryService.Application.Abstractions;
using Maliev.MessagingContracts.Contracts.Delivery;

namespace Maliev.DeliveryService.Tests.Fakes;

/// <summary>
/// Fake delivery PDF request publisher for tests.
/// </summary>
public sealed class FakeDeliveryPdfRequestPublisher : IDeliveryPdfRequestPublisher
{
    private readonly List<DeliveryNotePdfRequestedEvent> _published = [];

    /// <summary>
    /// Gets the published delivery-note PDF request events.
    /// </summary>
    public IReadOnlyList<DeliveryNotePdfRequestedEvent> Published => _published;

    /// <inheritdoc />
    public Task PublishAsync(DeliveryNotePdfRequestedEvent message, CancellationToken ct = default)
    {
        _published.Add(message);
        return Task.CompletedTask;
    }
}
