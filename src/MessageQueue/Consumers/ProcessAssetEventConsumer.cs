using Application.Features.Assets.Commands;
using Application.Features.Assets.Events;
using MassTransit;
using MediatR;

namespace MessageQueue.Consumers;

/// <summary>
///     MassTransit consumer that receives <see cref="ProcessAssetEvent" /> from SQS
///     and dispatches a <see cref="ProcessAssetCommand" /> through the MediatR pipeline.
///     The pipeline validates the command and enqueues it as a Hangfire job automatically.
///     This consumer is an infrastructure adapter — it only maps the event to a command
///     and delegates to MediatR. No business logic lives here.
/// </summary>
public sealed class ProcessAssetEventConsumer(
    ISender sender) : IConsumer<ProcessAssetEvent>
{
    public async Task Consume(ConsumeContext<ProcessAssetEvent> context)
    {
        var message = context.Message;

        var command = new ProcessAssetCommand
        {
            AssetId = message.AssetId,
            Code = message.Code,
            Name = message.Name,
            Value = message.Value
        };

        await sender.Send(command, context.CancellationToken);
    }
}
