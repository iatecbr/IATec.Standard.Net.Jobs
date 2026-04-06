using System.Reflection;
using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using HangFire.Jobs.Helpers;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using MediatR;

namespace HangFire.Jobs.Filters;

/// <summary>
///     Global Hangfire filter that propagates <see cref="AutomaticRetryAttribute" />,
///     <see cref="QueueAttribute" />, and <see cref="CommandDisplayNameAttribute" /> from the
///     command object to the job when enqueued via <c>ISender.Send(command)</c>.
///     <para>
///         <b>Queue:</b> The primary queue mechanism is
///         <see cref="Extensions.BackgroundJobClientExtensions.EnqueueCommand{TCommand}" />,
///         which sets <c>Job.Queue</c> natively via the Hangfire <c>Enqueue(queue, ...)</c> overload.
///         This filter serves as a safety net for queue assignment during state transitions
///         (e.g., retries) and for callers that use <c>Enqueue&lt;ISender&gt;</c> directly.
///     </para>
///     <para>
///         <b>Retry:</b> Reads <see cref="AutomaticRetryAttribute" /> from the command type
///         and applies retry logic with exponential backoff when the job fails.
///     </para>
///     <para>
///         <b>Display Name:</b> Reads <see cref="CommandDisplayNameAttribute" /> from the command type
///         and stores the formatted display name as a job parameter.
///     </para>
///     Register globally via <c>GlobalJobFilters.Filters.Add(new CommandAttributeJobFilter())</c>.
///     Also captures <see cref="PerformContext" /> via <see cref="IServerFilter" /> and stores it
///     in <see cref="PerformContextAccessor" /> so command handlers can access Hangfire Console.
/// </summary>
public sealed class CommandAttributeJobFilter : JobFilterAttribute, IClientFilter, IServerFilter, IElectStateFilter,
    IApplyStateFilter
{
    private const string RetryAttemptsKey = "CommandRetryAttempts";
    private const string QueueNameKey = "CommandQueue";
    private const string DisplayNameKey = "CommandDisplayName";

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        // No action needed
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        // No action needed
    }

    /// <summary>
    ///     Extracts the first <see cref="IBaseRequest" /> argument from the job expression
    ///     and reads Hangfire attributes from its type, storing them as job parameters.
    ///     Queue is set directly on <see cref="EnqueuedState" /> for the initial enqueue.
    /// </summary>
    public void OnCreating(CreatingContext context)
    {
        var commandType = FindCommandType(context.Job);

        if (commandType is null)
            return;

        // [AutomaticRetry] on command type → store attempts for OnStateElection
        var retryAttr = commandType.GetCustomAttribute<AutomaticRetryAttribute>();
        if (retryAttr is not null)
            context.SetJobParameter(RetryAttemptsKey, retryAttr.Attempts);

        // [Queue] on command type → set on initial state AND store for retries
        var queueAttr = commandType.GetCustomAttribute<QueueAttribute>();
        if (queueAttr is not null)
        {
            context.SetJobParameter(QueueNameKey, queueAttr.Queue);

            if (context.InitialState is EnqueuedState enqueuedState)
                enqueuedState.Queue = queueAttr.Queue;
        }

        // [CommandDisplayName] on command type
        var displayAttr = commandType.GetCustomAttribute<CommandDisplayNameAttribute>();
        if (displayAttr is not null)
        {
            var commandArg = FindCommandArg(context.Job);
            var formatted = string.Format(displayAttr.DisplayName, commandArg?.ToString() ?? string.Empty);
            context.SetJobParameter(DisplayNameKey, formatted);
        }
    }

    public void OnCreated(CreatedContext context)
    {
        // No action needed after creation
    }

    /// <summary>
    ///     Applies queue override and retry policy from command attributes during state election.
    ///     Queue: every time a job transitions to <see cref="EnqueuedState" /> (initial or after retry),
    ///     the queue is overridden to match the command's <see cref="QueueAttribute" />.
    ///     Retry: when a job enters <see cref="FailedState" />, retry policy from the command's
    ///     <see cref="AutomaticRetryAttribute" /> is applied with exponential backoff.
    /// </summary>
    public void OnStateElection(ElectStateContext context)
    {
        // Always apply queue from command on ANY EnqueuedState transition (initial + retries)
        ApplyQueueFromCommand(context);

        // Apply retry policy from command when job fails
        ApplyRetryFromCommand(context);
    }

    /// <summary>
    ///     Captures the <see cref="PerformContext" /> before the job method executes
    ///     and stores it in <see cref="PerformContextAccessor" /> via <see cref="AsyncLocal{T}" />.
    ///     This makes the context available to command handlers resolved by MediatR
    ///     during <c>ISender.Send(command)</c> execution inside the Hangfire worker.
    /// </summary>
    public void OnPerforming(PerformingContext context)
    {
        PerformContextAccessor.Set(context);
    }

    /// <summary>
    ///     Clears the <see cref="PerformContext" /> after the job method completes
    ///     to prevent leaking across jobs in the same worker thread.
    /// </summary>
    public void OnPerformed(PerformedContext context)
    {
        PerformContextAccessor.Clear();
    }

    private static void ApplyQueueFromCommand(ElectStateContext context)
    {
        if (context.CandidateState is not EnqueuedState enqueuedState)
            return;

        // Try reading from job args first (most reliable — no storage dependency)
        var commandType = FindCommandType(context.BackgroundJob.Job);
        if (commandType is not null)
        {
            var queueAttr = commandType.GetCustomAttribute<QueueAttribute>();
            if (queueAttr is not null)
            {
                enqueuedState.Queue = queueAttr.Queue;
                return;
            }
        }

        // Fallback to stored parameter (in case Job.Args are not deserialized)
        var queueName = context.GetJobParameter<string>(QueueNameKey);
        if (!string.IsNullOrEmpty(queueName))
            enqueuedState.Queue = queueName;
    }

    private static void ApplyRetryFromCommand(ElectStateContext context)
    {
        if (context.CandidateState is not FailedState)
            return;

        // Try reading from job args first
        var commandType = FindCommandType(context.BackgroundJob.Job);
        var maxAttempts = commandType?.GetCustomAttribute<AutomaticRetryAttribute>()?.Attempts;

        // Fallback to stored parameter
        maxAttempts ??= context.GetJobParameter<int?>(RetryAttemptsKey);

        if (!maxAttempts.HasValue)
            return;

        var currentRetry = context.GetJobParameter<int?>("RetryCount") ?? 0;

        if (currentRetry >= maxAttempts.Value)
            return;

        var delay = TimeSpan.FromSeconds(Math.Pow(2, currentRetry));

        context.CandidateState = new ScheduledState(delay)
        {
            Reason = $"Retry attempt {currentRetry + 1} of {maxAttempts.Value} (from command attribute)"
        };

        context.SetJobParameter("RetryCount", currentRetry + 1);
    }

    /// <summary>
    ///     Finds the command type from the job arguments.
    /// </summary>
    private static Type? FindCommandType(Job? job)
    {
        return FindCommandArg(job)?.GetType();
    }

    /// <summary>
    ///     Finds the first <see cref="IBaseRequest" /> argument from the job.
    /// </summary>
    private static object? FindCommandArg(Job? job)
    {
        if (job?.Args is null)
            return null;

        foreach (var arg in job.Args)
            if (arg is IBaseRequest request)
                return request;

        return null;
    }
}