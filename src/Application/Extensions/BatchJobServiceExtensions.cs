using System.Linq.Expressions;
using Application.Services;
using Hangfire;

namespace Application.Extensions;

/// <summary>
///     Extension methods to simplify job enqueuing within a monitored batch.
///     These methods extract the <see cref="IBatchAction" /> from the batch context object
///     passed to user code via <see cref="IBatchJobService.StartMonitoredBatch" />.
///     Usage example:
///     <code>
/// batchJobService.StartMonitoredBatch("Process Assets", batch =>
/// {
///     foreach (var asset in assets)
///     {
///         batch.EnqueueBatch&lt;ProcessAssetJob&gt;(
///             job => job.ExecuteAsync(asset, null, CancellationToken.None));
///     }
/// });
/// </code>
/// </summary>
public static class BatchJobServiceExtensions //TODO: move to library
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