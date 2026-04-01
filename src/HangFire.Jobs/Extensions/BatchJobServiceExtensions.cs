using System.Linq.Expressions;
using System.Reflection;
using FluentResults;
using Hangfire;
using HangFire.Jobs.Services;
using MediatR;

namespace HangFire.Jobs.Extensions;

/// <summary>
///     Extension methods to simplify job enqueuing within a monitored batch.
///     These methods extract the <see cref="IBatchAction" /> from the batch context object
///     passed to user code via <see cref="Domain.Contracts.Services.IBatchJobService.StartMonitoredBatch" />.
///     Usage example:
///     <code>
/// batchJobService.StartMonitoredBatch("Process Assets", (batch, batchKeyValue) =>
/// {
///     foreach (var command in commands)
///     {
///         batch.EnqueueCommandBatch(command, CancellationToken.None);
///     }
/// });
/// </code>
/// </summary>
public static class BatchJobServiceExtensions
{
    /// <summary>
    ///     Enqueues a fire-and-forget job within a monitored batch.
    /// </summary>
    /// <typeparam name="T">The job type to be resolved from DI.</typeparam>
    /// <param name="batchContext">The batch context object received in the StartMonitoredBatch action.</param>
    /// <param name="methodCall">Expression representing the job method to call.</param>
    /// <returns>The enqueued job ID.</returns>
    public static string EnqueueBatch<T>(this object batchContext, Expression<Action<T>> methodCall)
        where T : class
    {
        var (action, counter) = Unwrap(batchContext);
        var jobId = action.Enqueue(methodCall);
        counter();
        return jobId;
    }

    /// <summary>
    ///     Enqueues an async fire-and-forget job within a monitored batch.
    /// </summary>
    /// <typeparam name="T">The job type to be resolved from DI.</typeparam>
    /// <param name="batchContext">The batch context object received in the StartMonitoredBatch action.</param>
    /// <param name="methodCall">Expression representing the async job method to call.</param>
    /// <returns>The enqueued job ID.</returns>
    public static string EnqueueBatch<T>(this object batchContext, Expression<Func<T, Task>> methodCall)
        where T : class
    {
        var (action, counter) = Unwrap(batchContext);
        var jobId = action.Enqueue(methodCall);
        counter();
        return jobId;
    }

    /// <summary>
    ///     Schedules a job to be enqueued after a delay, within a monitored batch.
    /// </summary>
    /// <typeparam name="T">The job type to be resolved from DI.</typeparam>
    /// <param name="batchContext">The batch context object received in the StartMonitoredBatch action.</param>
    /// <param name="methodCall">Expression representing the job method to call.</param>
    /// <param name="delay">The delay before the job should be enqueued.</param>
    /// <returns>The scheduled job ID.</returns>
    public static string ScheduleBatch<T>(this object batchContext, Expression<Action<T>> methodCall, TimeSpan delay)
        where T : class
    {
        var (action, counter) = Unwrap(batchContext);
        var jobId = action.Schedule(methodCall, delay);
        counter();
        return jobId;
    }

    /// <summary>
    ///     Schedules an async job to be enqueued after a delay, within a monitored batch.
    /// </summary>
    /// <typeparam name="T">The job type to be resolved from DI.</typeparam>
    /// <param name="batchContext">The batch context object received in the StartMonitoredBatch action.</param>
    /// <param name="methodCall">Expression representing the async job method to call.</param>
    /// <param name="delay">The delay before the job should be enqueued.</param>
    /// <returns>The scheduled job ID.</returns>
    public static string ScheduleBatch<T>(this object batchContext, Expression<Func<T, Task>> methodCall,
        TimeSpan delay)
        where T : class
    {
        var (action, counter) = Unwrap(batchContext);
        var jobId = action.Schedule(methodCall, delay);
        counter();
        return jobId;
    }

    /// <summary>
    ///     Adds a continuation job that runs after the specified antecedent job completes, within a batch.
    /// </summary>
    /// <typeparam name="T">The job type to be resolved from DI.</typeparam>
    /// <param name="batchContext">The batch context object received in the StartMonitoredBatch action.</param>
    /// <param name="parentJobId">The ID of the job to wait for.</param>
    /// <param name="methodCall">Expression representing the job method to call.</param>
    /// <returns>The continuation job ID.</returns>
    public static string ContinueJobWithBatch<T>(this object batchContext, string parentJobId,
        Expression<Action<T>> methodCall)
        where T : class
    {
        var (action, counter) = Unwrap(batchContext);
        var jobId = action.ContinueJobWith(parentJobId, methodCall);
        counter();
        return jobId;
    }

    /// <summary>
    ///     Adds an async continuation job that runs after the specified antecedent job completes, within a batch.
    /// </summary>
    /// <typeparam name="T">The job type to be resolved from DI.</typeparam>
    /// <param name="batchContext">The batch context object received in the StartMonitoredBatch action.</param>
    /// <param name="parentJobId">The ID of the job to wait for.</param>
    /// <param name="methodCall">Expression representing the async job method to call.</param>
    /// <returns>The continuation job ID.</returns>
    public static string ContinueJobWithBatch<T>(this object batchContext, string parentJobId,
        Expression<Func<T, Task>> methodCall)
        where T : class
    {
        var (action, counter) = Unwrap(batchContext);
        var jobId = action.ContinueJobWith(parentJobId, methodCall);
        counter();
        return jobId;
    }

    /// <summary>
    ///     Enqueues a MediatR command within a monitored batch via <c>ISender.Send(command)</c>.
    ///     Reads <see cref="QueueAttribute" /> from <typeparamref name="TCommand" /> to set the queue
    ///     on the batch action, matching the behavior of
    ///     <see cref="BackgroundJobClientExtensions.EnqueueCommand{TCommand}" /> for standalone jobs.
    ///     Usage:
    ///     <code>
    /// batch.EnqueueCommandBatch(command, cancellationToken);
    /// </code>
    /// </summary>
    /// <typeparam name="TCommand">
    ///     The MediatR command type. May have <see cref="QueueAttribute" />,
    ///     <see cref="AutomaticRetryAttribute" />, and/or
    ///     <see cref="Filters.CommandDisplayNameAttribute" />.
    /// </typeparam>
    /// <param name="batchContext">The batch context object received in the StartMonitoredBatch action.</param>
    /// <param name="command">The command instance to enqueue inside the batch.</param>
    /// <param name="cancellationToken">Cancellation token passed to <c>ISender.Send</c>.</param>
    /// <returns>The enqueued job ID.</returns>
    public static string EnqueueCommandBatch<TCommand>(
        this object batchContext,
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : IRequest<Result>
    {
        var (action, counter) = Unwrap(batchContext);

        var queue = typeof(TCommand).GetCustomAttribute<QueueAttribute>()?.Queue;

        var jobId = queue is not null
            ? action.Enqueue<ISender>(queue, sender => sender.Send(command, cancellationToken))
            : action.Enqueue<ISender>(sender => sender.Send(command, cancellationToken));

        counter();
        return jobId;
    }

    private static (IBatchAction Action, Action IncrementCount) Unwrap(object batchContext)
    {
        if (batchContext is JobCountingBatchAction counting)
            return (counting.Inner, counting.IncrementCount);

        if (batchContext is IBatchAction directAction)
            return (directAction, () => { });

        throw new InvalidOperationException(
            $"Expected batch context of type {nameof(JobCountingBatchAction)} or {nameof(IBatchAction)}, " +
            $"but received {batchContext.GetType().Name}. " +
            "Ensure you are using the batch context provided by IBatchJobService.StartMonitoredBatch().");
    }
}
