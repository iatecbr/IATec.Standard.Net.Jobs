using Domain.Contracts.Helpers;
using Domain.Models.JobAggregate;
using FluentResults;
using HangFire.Jobs.Base;
using HangFire.Jobs.Contracts;

namespace Application.Features.Assets.Commands;

/// <summary>
///     Handler for <see cref="ProcessAssetCommand" />.
///     Runs inside the Hangfire worker when enqueued via
///     <c>backgroundJobClient.EnqueueCommand(command)</c>.
///     Hangfire attributes are defined on <see cref="ProcessAssetCommand" /> and propagated
///     by <see cref="HangFire.Jobs.Filters.CommandAttributeJobFilter" />.
///     PerformContext is available via <see cref="IPerformContextAccessor" /> — Hangfire Console works.
/// </summary>
public sealed class ProcessAssetCommandHandler(
    IJobHelper jobHelper,
    IPerformContextAccessor performContextAccessor)
    : BaseCommand<ProcessAssetCommand>(jobHelper, performContextAccessor)
{
    /// <inheritdoc />
    protected override async Task<Result> HandleAsync(
        ProcessAssetCommand request, CancellationToken cancellationToken)
    {
        NotifyInfo($"Processing asset {request.Code} ({request.Name})...");

        // Simulate multi-step processing with ProgressBar to demonstrate full
        // Hangfire Console support. The progress bar is visible in the Dashboard Console panel.
        const int totalSteps = 5;

        UpdateProgressWithMessage(0, totalSteps, "Validating asset metadata...");
        await Task.Delay(50, cancellationToken);

        UpdateProgressWithMessage(1, totalSteps, "Downloading asset data...");
        await Task.Delay(50, cancellationToken);

        UpdateProgressWithMessage(2, totalSteps, "Transforming asset format...");
        await Task.Delay(50, cancellationToken);

        UpdateProgressWithMessage(3, totalSteps, "Persisting to storage...");
        await Task.Delay(50, cancellationToken);

        UpdateProgressWithMessage(4, totalSteps, "Running post-processing checks...");
        await Task.Delay(50, cancellationToken);

        UpdateProgressWithMessage(totalSteps, totalSteps, "Complete");

        if (!string.IsNullOrEmpty(request.BatchKeyValue))
        {
            var batchKey = BatchKey.FromRawValue(request.BatchKeyValue);
            JobHelper.IncrementBatchProgress(batchKey, PerformContext, false);
        }

        NotifyInfo($"Asset {request.Code} processed successfully.");

        return Result.Ok();
    }
}
