using FluentResults;
using Hangfire;
using HangFire.Jobs.Constants;
using HangFire.Jobs.Filters;
using MediatR;

namespace Application.Features.Assets.Commands;

/// <summary>
///     Command to create a monitored batch of asset processing jobs.
///     When enqueued via <c>backgroundJobClient.EnqueueCommand(command)</c>,
///     the handler creates a Hangfire Pro batch with one <see cref="ProcessAssetCommand" />
///     per asset. A <see cref="HangFire.Jobs.Commands.MonitorBatchCommand" /> is automatically
///     enqueued as a standalone polling job to display a real-time progress bar in the Hangfire Console.
///     Pipeline:
///     1. ProcessAssetBatchCommand handler creates the batch and enqueues N x ProcessAssetCommand
///     2. Each ProcessAssetCommand runs independently inside the batch
///     3. MonitorBatchCommand polls progress in real-time via Redis (standalone job)
///     4. Batch progress can be queried at any time via IBatchJobService.GetBatchMonitorResult()
///     All properties are positional constructor parameters so that Newtonsoft.Json (used by
///     Hangfire for serialization) can correctly round-trip the record through its constructor.
/// </summary>
[AutomaticRetry(Attempts = JobRetryPolicyConstant.NoRetry)]
[Queue(JobRetryPolicyConstant.DefaultQueue)]
[CommandDisplayName("Process Asset Batch: {0}")]
public sealed record ProcessAssetBatchCommand(
    IReadOnlyCollection<ProcessAssetCommand> Commands) : IRequest<Result>
{
    public override string ToString()
    {
        return $"{Commands.Count} assets";
    }
}