using Domain.Contracts.Bus;

namespace Application.Features.Assets.Events;

/// <summary>
///     Integration event published when an asset needs to be processed.
///     Received via MassTransit (Amazon SQS) and handled by
///     <c>MessageQueue.Consumers.ProcessAssetEventConsumer</c>,
///     which dispatches a <see cref="Commands.ProcessAssetCommand" /> through the MediatR pipeline.
/// </summary>
public sealed record ProcessAssetEvent : IIntegrationEvent
{
    public Guid AssetId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal Value { get; init; }
}