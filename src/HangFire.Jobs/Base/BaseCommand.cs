using Domain.Contracts.Helpers;
using FluentResults;
using Hangfire.Console;
using HangFire.Jobs.Contracts;
using Hangfire.Server;
using MediatR;

namespace HangFire.Jobs.Base;

/// <summary>
///     Abstract base class for MediatR command handlers that run inside Hangfire workers
///     via <c>ISender.Send(command)</c>.
///     Implements a Template Method lifecycle (Start → HandleAsync → Finish/Error → Finally)
///     with full Hangfire Console support.
///     The <see cref="PerformContext" /> is obtained from <see cref="IPerformContextAccessor" />,
///     which is populated by <see cref="Filters.CommandAttributeJobFilter.OnPerforming" />.
///     Subclasses only need to implement <see cref="HandleAsync" />.
/// </summary>
/// <typeparam name="TCommand">The MediatR command type (must implement <see cref="IRequest{Result}" />).</typeparam>
public abstract class BaseCommand<TCommand>(
    IJobHelper jobHelper,
    IPerformContextAccessor performContextAccessor,
    string? messagePrefix = null) : IRequestHandler<TCommand, Result>
    where TCommand : IRequest<Result>
{
    private readonly string _messageLogPrefix = messagePrefix ?? string.Empty;

    protected IJobHelper JobHelper { get; } = jobHelper;

    /// <summary>
    ///     The current Hangfire <see cref="PerformContext" />.
    ///     Available when running inside a Hangfire worker; null in unit tests.
    /// </summary>
    protected PerformContext? PerformContext => performContextAccessor.PerformContext;

    /// <summary>
    ///     MediatR entry point. Runs the full lifecycle: Start → HandleAsync → Finish/Error → Finally.
    ///     Business errors (<see cref="Result.Fail(string)" />) are logged but do NOT throw — no retry.
    ///     When running inside Hangfire, a business failure returns <see cref="Result" /> without throwing,
    ///     so Hangfire considers the job Succeeded (no retry). This is intentional — business errors
    ///     should not be retried. Infrastructure exceptions ARE re-thrown so Hangfire retries
    ///     via <c>[AutomaticRetry]</c>.
    /// </summary>
    public async Task<Result> Handle(TCommand request, CancellationToken cancellationToken)
    {
        var context = PerformContext;

        try
        {
            JobHelper.Start(context);
            NotifyInfo($"Starting {GetType().Name}...");

            var result = await HandleAsync(request, cancellationToken);

            if (result.IsFailed)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Message));
                JobHelper.Error(context, errors);
                NotifyWarn($"Command {GetType().Name} failed by business rule: {errors}");
                return result;
            }

            JobHelper.Finish(context);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            JobHelper.Error(context, "Command was cancelled");
            NotifyWarn($"Command {GetType().Name} was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            JobHelper.Error(context, ex);
            NotifyErrorAndContinue($"Unexpected error in command {GetType().Name}: {ex.Message}");
            throw;
        }
        finally
        {
            JobHelper.Finally(context);
        }
    }

    /// <summary>
    ///     Implement this with the actual business logic.
    ///     Return <see cref="Result.Ok()" /> on success or <see cref="Result.Fail(string)" /> on business error.
    /// </summary>
    protected abstract Task<Result> HandleAsync(TCommand request, CancellationToken cancellationToken);

    /// <summary>
    ///     Logs an informational message to Hangfire Console.
    /// </summary>
    protected void NotifyInfo(string info)
    {
        var message = $"{_messageLogPrefix}{info}";
        JobHelper.Info(PerformContext, message);
    }

    /// <summary>
    ///     Logs a debug-level message.
    /// </summary>
    protected void NotifyLog(string info)
    {
        var message = $"{_messageLogPrefix}{info}";
        JobHelper.Log(PerformContext, message);
    }

    /// <summary>
    ///     Logs a warning message to Hangfire Console.
    /// </summary>
    protected void NotifyWarn(string info)
    {
        var context = PerformContext;

        if (context is not null)
            context.SetTextColor(ConsoleTextColor.Yellow);

        var message = $"{_messageLogPrefix}{info}";
        JobHelper.Info(context, message);

        if (context is not null)
            context.ResetTextColor();
    }

    /// <summary>
    ///     Logs an error but allows the command to continue processing.
    /// </summary>
    protected void NotifyErrorAndContinue(string errorMessage)
    {
        var message = $"{_messageLogPrefix}{errorMessage}";
        JobHelper.Error(PerformContext, message);
    }

    /// <summary>
    ///     Logs an error and returns a failed Result to stop the command.
    ///     Business error — will NOT be retried by Hangfire.
    /// </summary>
    protected Result NotifyErrorAndStop(string errorMessage)
    {
        var message = $"{_messageLogPrefix}{errorMessage}";
        JobHelper.Error(PerformContext, message);
        return Result.Fail(errorMessage);
    }

    /// <summary>
    ///     Sets the progress bar to the specified percentage (0-100).
    ///     Convenience wrapper — the <see cref="PerformContext" /> is resolved automatically
    ///     from <see cref="IPerformContextAccessor" />.
    /// </summary>
    protected void NotifyProgress(int percentage)
    {
        JobHelper.ProgressBar(PerformContext, percentage);
    }

    /// <summary>
    ///     Updates the progress bar based on current/total item count.
    ///     Sets job parameters "Progress" and "Count" for Dashboard visibility.
    /// </summary>
    protected void UpdateProgress(int current, int total)
    {
        JobHelper.UpdateProgress(PerformContext, current, total);
    }

    /// <summary>
    ///     Updates the progress bar with current/total count and a custom message.
    ///     Sets job parameters "Progress", "Count", and "Message" for Dashboard visibility.
    /// </summary>
    protected void UpdateProgressWithMessage(int current, int total, string message)
    {
        JobHelper.UpdateProgressWithMessage(PerformContext, current, total, message);
    }
}