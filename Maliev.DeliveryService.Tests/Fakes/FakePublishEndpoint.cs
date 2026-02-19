using MassTransit;

namespace Maliev.DeliveryService.Tests.Fakes;

/// <summary>
/// Fake implementation of IPublishEndpoint for testing event publishing (no mocking libraries)
/// </summary>
public class FakePublishEndpoint : IPublishEndpoint
{
    private readonly List<object> _publishedMessages = new();

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        _publishedMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        _publishedMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        _publishedMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish(object message, CancellationToken cancellationToken = default)
    {
        _publishedMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        _publishedMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        _publishedMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        _publishedMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class
    {
        _publishedMessages.Add(values);
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        _publishedMessages.Add(values);
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        _publishedMessages.Add(values);
        return Task.CompletedTask;
    }

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        throw new NotImplementedException();
    }

    // Test helper methods
    public List<T> GetPublishedMessages<T>() where T : class
    {
        return _publishedMessages.OfType<T>().ToList();
    }

    public bool WasPublished<T>() where T : class
    {
        return _publishedMessages.Any(m => m is T);
    }

    public void Clear()
    {
        _publishedMessages.Clear();
    }
}
