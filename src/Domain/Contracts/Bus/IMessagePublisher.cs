namespace Domain.Contracts.Bus;

/// <summary>
///     Abstraction for publishing integration events to the message bus.
///     Decouples domain/application layers from the messaging infrastructure (MassTransit, SQS, etc.).
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync<T>(
        T integrationEvent,
        CancellationToken cancellationToken = default
    ) where T : class, IIntegrationEvent;
}