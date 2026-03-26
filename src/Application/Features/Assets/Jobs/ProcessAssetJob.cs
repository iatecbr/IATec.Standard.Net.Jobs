using Application.Base;
using Application.Constants;
using Domain.Contracts.Helpers;
using Domain.Models.AssetAggregate.Jobs;
using Domain.Models.JobAggregate;
using FluentResults;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace Application.Features.Assets.Jobs;

/// <summary>
///     Hangfire job that processes an asset.
///     Inherits from BaseJob, which automatically handles Start -> RunAsync -> Finally lifecycle.
///     When running inside a monitored batch, reports progress via <see cref="IJobHelper.IncrementBatchProgress" />.
/// </summary>
[AutomaticRetry(Attempts = JobRetryPolicyConstant.DefaultRetryAttempts)]
[Queue(JobRetryPolicyConstant.DefaultQueue)]
public class ProcessAssetJob(
    IJobHelper jobHelper,
    ILogger<ProcessAssetJob> logger) : BaseJob(jobHelper, logger)
{
    // Stored by the Hangfire entry point before the base lifecycle runs.
    private ProcessAssetDataJobDto _jobData = null!;

    /// <summary>
    ///     Called by Hangfire. The job data is passed as a parameter.
    ///     Stores the data in a field, then delegates to the base class lifecycle
    ///     (Start -> RunAsync -> Finally).
    ///     PerformContext is injected automatically by Hangfire at runtime.
    /// </summary>
    [JobDisplayName("Process Asset: {0}")]
    public Task ExecuteAsync(
        ProcessAssetDataJobDto jobData,
        PerformContext? performContext,
        CancellationToken cancellationToken)
    {
        _jobData = jobData;
        return base.ExecuteAsync(performContext, cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task<Result> RunAsync(PerformContext? performContext, CancellationToken cancellationToken)
    {
        NotifyInfo(performContext, $"Processing asset {_jobData.AssetId} - {_jobData.Name}");

        var failed = false;
        try
        {
            // TODO: Implement actual asset processing logic
            await Task.Delay(100, cancellationToken); // Simulate work

            NotifyInfo(performContext, $"Asset {_jobData.AssetId} processed successfully");

            return Result.Ok();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            failed = true;
            throw;
        }
        finally
        {
            // Report progress to batch if this job is part of one
            if (!string.IsNullOrEmpty(_jobData.BatchKeyValue))
            {
                var batchKey = BatchKey.FromRawValue(_jobData.BatchKeyValue);
                JobHelper.IncrementBatchProgress(batchKey, performContext, failed);
            }
        }
    }
}