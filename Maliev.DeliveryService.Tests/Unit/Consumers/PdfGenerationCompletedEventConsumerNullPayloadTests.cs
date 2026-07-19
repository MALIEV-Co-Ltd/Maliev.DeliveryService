using Maliev.DeliveryService.Infrastructure.Consumers;
using Maliev.MessagingContracts.Contracts.Pdf;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Consumers;

/// <summary>
/// Unit tests for malformed PDF completion events.
/// </summary>
public sealed class PdfGenerationCompletedEventConsumerNullPayloadTests
{
    /// <summary>
    /// Ensures malformed PDF completion events are ignored before database access.
    /// </summary>
    [Fact]
    public async Task Consume_WithoutPayload_IsIgnored()
    {
        var consumer = new PdfGenerationCompletedEventConsumer(
            null!,
            Mock.Of<ILogger<PdfGenerationCompletedEventConsumer>>());

        await consumer.Consume(CreateContext(new PdfGenerationCompletedEvent { Payload = null! }).Object);
    }

    private static Mock<ConsumeContext<T>> CreateContext<T>(T message)
        where T : class
    {
        var context = new Mock<ConsumeContext<T>>();
        context.Setup(c => c.Message).Returns(message);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }
}
