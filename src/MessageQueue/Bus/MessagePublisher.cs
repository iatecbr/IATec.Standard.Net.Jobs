using Domain.Contracts.Bus;
using MassTransit;

namespace MessageQueue.Bus;

/// <summary>
///     MassTransit implementation of <see cref="IMessagePublisher" />.
///     Wraps <see cref="IPublishEndpoint" /> to decouple Application layer from MassTransit.
/// </summary>
public class MessagePublisher(IPublishEndpoint bus) : IMessagePublisher
{
    public async Task PublishAsync<T>(
        T integrationEvent,
        CancellationToken cancellationToken = default) where T : class, IIntegrationEvent
    {
        await bus.Publish(integrationEvent, cancellationToken);
    }
}