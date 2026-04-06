using Application.Features.Assets.Events;
using MassTransit;

namespace Integration.Tests.Helpers;

public sealed class TestProcessAssetEventConsumer(
    ConsumedMessageStore store) : IConsumer<ProcessAssetEvent>
{
    public Task Consume(ConsumeContext<ProcessAssetEvent> context)
    {
        store.Add(context.Message);
        return Task.CompletedTask;
    }
}