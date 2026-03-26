using Application.Base;
using Application.Constants;
using Application.Extensions;
using Domain.Contracts.Helpers;
using Domain.Contracts.Services;
using Domain.Models.AssetAggregate.Jobs;
using FluentResults;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace Application.Features.Assets.Jobs;

/// <summary>
///     Hangfire job that creates a monitored batch of <see cref="ProcessAssetJob" /> executions.
///     Each asset in the input list is enqueued as an individual job inside a Hangfire Pro batch.
///     A <see cref="Application.Jobs.BatchMonitorJob" /> is automatically enqueued as a standalone
///     polling job to display a real-time progress bar in the Hangfire Console.
///     This demonstrates the full monitoring pipeline:
///     1. ProcessAssetBatchJob receives the list and creates the batch (this job)
///     2. Each ProcessAssetJob runs independently inside the batch (individual monitoring via BaseJob lifecycle)
///     3. BatchMonitorJob polls progress in real-time via Redis (standalone job, not a continuation)
///     4. Batch progress can be queried at any time via IBatchJobService.GetBatchMonitorResult()
/// </summary>
[AutomaticRetry(Attempts = JobRetryPolicyConstant.NoRetry)]
[Queue(JobRetryPolicyConstant.DefaultQueue)]
public class ProcessAssetBatchJob(
    IJobHelper jobHelper,
    ILogger<ProcessAssetBatchJob> logger,
    IBatchJobService batchJobService) : BaseJob(jobHelper, logger)
{
    // Stored by the Hangfire entry point before the base lifecycle runs.
    private ProcessAssetDataJobDto[] _assets = [];

    /// <summary>
    ///     Called by Hangfire. Receives the list of assets to process as a batch.
    ///     Stores the data in a field, then delegates to the base class lifecycle
    ///     (Start -> RunAsync -> Finally).
    ///     PerformContext is injected automatically by Hangfire at runtime.
    /// </summary>
    [JobDisplayName("Process Asset Batch")]
    public Task ExecuteAsync(
        ProcessAssetDataJobDto[] assets,
        PerformContext? performContext,
        CancellationToken cancellationToken)
    {
        _assets = assets;
        return base.ExecuteAsync(performContext, cancellationToken);
    }

    /// <inheritdoc />
    protected override Task<Result> RunAsync(PerformContext? performContext, CancellationToken cancellationToken)
    {
        if (_assets.Length == 0)
            return Task.FromResult(
                NotifyErrorAndStop(performContext, "No assets provided for batch processing."));

        NotifyInfo(performContext, $"Creating monitored batch for {_assets.Length} assets...");

        // BatchJobService handles everything: BatchKey creation, InitializeBatchProgress,
        // StoreMetadata (including BatchKeyValue), and enqueueing BatchMonitorJob.
        var batchInfo = batchJobService.StartMonitoredBatch(
            $"Process {_assets.Length} Assets",
            (batch, batchKeyValue) =>
            {
                foreach (var asset in _assets)
                {
                    // Pass the batch key so each job can report progress
                    var assetWithBatchKey = asset with { BatchKeyValue = batchKeyValue };

                    batch.EnqueueBatch<ProcessAssetJob>(job =>
                        job.ExecuteAsync(assetWithBatchKey, null, CancellationToken.None));
                }
            });

        NotifyInfo(performContext,
            $"Batch created successfully | BatchId: {batchInfo.BatchId} | BatchKey: {batchInfo.BatchKeyValue} | Total jobs: {_assets.Length}");

        logger.LogInformation(
            "ProcessAssetBatchJob created batch {BatchId} with {TotalJobs} jobs, BatchKey={BatchKey}",
            batchInfo.BatchId, _assets.Length, batchInfo.BatchKeyValue);

        return Task.FromResult(Result.Ok());
    }
}