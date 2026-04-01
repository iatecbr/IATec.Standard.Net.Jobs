using FluentResults;
using Hangfire;
using HangFire.Jobs.Constants;
using HangFire.Jobs.Filters;
using MediatR;

namespace HangFire.Jobs.Commands;

/// <summary>
///     Generic command to monitor any batch's progress in real-time via a Hangfire Console progress bar.
///     Enqueued as a standalone job (not a continuation) alongside the batch — it polls the
///     batch progress Redis hash until all jobs complete, updating the progress bar on each tick.
///     When done, records total execution time and final status.
///     This command is automatically enqueued by <see cref="Services.BatchJobService" />.
///     It should NOT be enqueued manually.
/// </summary>
[AutomaticRetry(Attempts = JobRetryPolicyConstant.NoRetry)]
[Queue(JobRetryPolicyConstant.DefaultQueue)]
[CommandDisplayName("Monitor Batch: {0}")]
public sealed record MonitorBatchCommand : IRequest<Result>
{
    /// <summary>
    ///     The Hangfire Pro batch ID being monitored.
    /// </summary>
    public string BatchId { get; init; } = string.Empty;

    /// <summary>
    ///     Human-readable name of the batch.
    /// </summary>
    public string BatchName { get; init; } = string.Empty;

    /// <summary>
    ///     The batch progress Redis key for reading completed/failed counts.
    /// </summary>
    public string BatchKeyValue { get; init; } = string.Empty;

    public override string ToString() => BatchName;
}
