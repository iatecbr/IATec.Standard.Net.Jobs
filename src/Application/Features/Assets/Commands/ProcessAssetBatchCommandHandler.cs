using Domain.Contracts.Helpers;
using Domain.Contracts.Services;
using FluentResults;
using HangFire.Jobs.Base;
using HangFire.Jobs.Contracts;
using HangFire.Jobs.Extensions;

namespace Application.Features.Assets.Commands;

/// <summary>
///     Handler for <see cref="ProcessAssetBatchCommand" />.
///     Creates a monitored Hangfire Pro batch with one <see cref="ProcessAssetCommand" /> per asset.
///     Each asset is enqueued via <see cref="BatchJobServiceExtensions.EnqueueCommandBatch{TCommand}" />
///     which calls <c>ISender.Send(command)</c> inside the batch, preserving queue attributes.
///     A <see cref="HangFire.Jobs.Commands.MonitorBatchCommand" /> is automatically enqueued by
///     <see cref="IBatchJobService.StartMonitoredBatch" /> to poll progress in real-time.
/// </summary>
public sealed class ProcessAssetBatchCommandHandler(
    IJobHelper jobHelper,
    IPerformContextAccessor performContextAccessor,
    IBatchJobService batchJobService)
    : BaseCommand<ProcessAssetBatchCommand>(jobHelper, performContextAccessor)
{
    /// <inheritdoc />
    protected override Task<Result> HandleAsync(
        ProcessAssetBatchCommand request, CancellationToken cancellationToken)
    {
        if (request.Commands.Count == 0)
            return Task.FromResult(
                NotifyErrorAndStop("No assets provided for batch processing."));

        NotifyInfo($"Creating monitored batch for {request.Commands.Count} assets...");

        // BatchJobService handles everything: BatchKey creation, InitializeBatchProgress,
        // StoreMetadata (including BatchKeyValue), and enqueueing MonitorBatchCommand.
        var batchInfo = batchJobService.StartMonitoredBatch(
            $"Process {request.Commands.Count} Assets",
            (batch, batchKeyValue) =>
            {
                foreach (var command in request.Commands)
                {
                    // Pass the batch key so each handler can report progress
                    var commandWithBatchKey = command with { BatchKeyValue = batchKeyValue };
                    batch.EnqueueCommandBatch(commandWithBatchKey, cancellationToken);
                }
            });

        NotifyInfo(
            $"Batch created successfully | BatchId: {batchInfo.BatchId} | BatchKey: {batchInfo.BatchKeyValue} | Total jobs: {request.Commands.Count}");

        return Task.FromResult(Result.Ok());
    }
}
