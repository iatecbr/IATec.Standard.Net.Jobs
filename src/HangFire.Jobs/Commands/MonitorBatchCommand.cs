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
///     All properties are positional constructor parameters so that Newtonsoft.Json (used by
///     Hangfire for serialization) can correctly round-trip the record through its constructor.
/// </summary>
[AutomaticRetry(Attempts = JobRetryPolicyConstant.NoRetry)]
[Queue(JobRetryPolicyConstant.DefaultQueue)]
[CommandDisplayName("Monitor Batch: {0}")]
public sealed record MonitorBatchCommand(
    string BatchId,
    string BatchName,
    string BatchKeyValue) : IRequest<Result>
{
    public override string ToString()
    {
        return BatchName;
    }
}