using System.Reflection;
using FluentResults;
using Hangfire;
using MediatR;

namespace HangFire.Jobs.Extensions;

/// <summary>
///     Extension methods for <see cref="IBackgroundJobClient" /> to enqueue MediatR commands
///     via <c>ISender.Send(command)</c> while preserving Hangfire queue assignment.
///     When using <c>backgroundJobClient.Enqueue&lt;ISender&gt;(s => s.Send(command))</c> directly,
///     Hangfire sets <c>Job.Queue = null</c> because <see cref="ISender" /> has no
///     <see cref="QueueAttribute" />. The Hangfire Dashboard reads <c>Job.Queue</c> to display
///     which queue a job belongs to — so the queue appears missing.
///     These extension methods read <see cref="QueueAttribute" /> from the command type
///     and use the native <c>Enqueue&lt;T&gt;(string queue, ...)</c> overload that sets
///     <c>Job.Queue</c> directly, ensuring full parity with Pattern 1 (BaseJob) jobs.
/// </summary>
public static class BackgroundJobClientExtensions
{
    /// <summary>
    ///     Enqueues a MediatR command via <c>ISender.Send(command)</c> as a Hangfire fire-and-forget job.
    ///     Reads <see cref="QueueAttribute" /> from <typeparamref name="TCommand" /> to set the queue
    ///     on the <see cref="Hangfire.Common.Job" /> object (not just <see cref="Hangfire.States.EnqueuedState" />).
    ///     Falls back to <see cref="Hangfire.States.EnqueuedState.DefaultQueue" /> if no <see cref="QueueAttribute" /> is present.
    /// </summary>
    /// <typeparam name="TCommand">
    ///     The MediatR command type. May have <see cref="QueueAttribute" />,
    ///     <see cref="AutomaticRetryAttribute" />, and/or
    ///     <see cref="Filters.CommandDisplayNameAttribute" />.
    /// </typeparam>
    /// <param name="client">The Hangfire background job client.</param>
    /// <param name="command">The command instance to enqueue.</param>
    /// <param name="cancellationToken">Cancellation token passed to <c>ISender.Send</c>.</param>
    /// <returns>The Hangfire job ID.</returns>
    public static string EnqueueCommand<TCommand>(
        this IBackgroundJobClient client,
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : IRequest<Result>
    {
        var queue = typeof(TCommand).GetCustomAttribute<QueueAttribute>()?.Queue;

        if (queue is not null)
        {
            return client.Enqueue<ISender>(queue,
                sender => sender.Send(command, cancellationToken));
        }

        return client.Enqueue<ISender>(
            sender => sender.Send(command, cancellationToken));
    }
}
