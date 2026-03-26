using Domain.Contracts.Helpers;
using FluentResults;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace Application.Base;

/// <summary>
///     Abstract base class for Hangfire jobs implementing the Template Method pattern.
///     Subclasses only need to implement <see cref="RunAsync" />.
///     The execution lifecycle (Start -> RunAsync -> Finally) is managed automatically.
///     Business errors are communicated via <see cref="Result" /> (FluentResults), not exceptions.
/// </summary>
public abstract class BaseJob(IJobHelper jobHelper, ILogger logger, string? messagePrefix = null)
{
    private readonly string _messageLogPrefix = messagePrefix ?? string.Empty;

    // Protected so subclasses can call helper methods, but sealed enough to prevent shadowing.
    // The field name matches the 7You/Team convention but is injected via primary constructor.
    protected IJobHelper JobHelper { get; } = jobHelper;

    /// <summary>
    ///     Entry point called by Hangfire. Implements the template method pattern:
    ///     Start -> RunAsync -> Finally (always runs).
    ///     If RunAsync returns a failed Result, the job is logged as failed and marked as completed
    ///     (no retry), since it represents a business validation failure.
    /// </summary>
    public async Task ExecuteAsync(PerformContext? performContext, CancellationToken cancellationToken)
    {
        try
        {
            JobHelper.Start(performContext);

            var result = await RunAsync(performContext, cancellationToken);

            if (result.IsFailed)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Message));
                JobHelper.Error(performContext, errors);
                logger.LogWarning("Job {JobName} failed by business rule: {Errors}",
                    GetType().Name, errors);
                return;
            }

            JobHelper.Finish(performContext);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            JobHelper.Error(performContext, "Job was cancelled");
            logger.LogInformation("Job {JobName} was cancelled", GetType().Name);
            throw;
        }
        catch (Exception ex)
        {
            JobHelper.Error(performContext, ex);
            logger.LogError(ex, "Unexpected error in job {JobName}", GetType().Name);
            throw;
        }
        finally
        {
            JobHelper.Finally(performContext);
        }
    }

    /// <summary>
    ///     Implement this method with the actual job logic.
    ///     Called automatically by <see cref="ExecuteAsync" />.
    ///     Return <see cref="Result.Ok()" /> on success or <see cref="Result.Fail(string)" /> on business error.
    /// </summary>
    protected abstract Task<Result> RunAsync(PerformContext? performContext, CancellationToken cancellationToken);

    /// <summary>
    ///     Logs an informational message to the Hangfire console.
    /// </summary>
    protected void NotifyInfo(PerformContext? performContext, string info)
    {
        JobHelper.Info(performContext, $"{_messageLogPrefix}{info}");
    }

    /// <summary>
    ///     Logs a message without console output.
    /// </summary>
    protected void NotifyLog(PerformContext? performContext, string info)
    {
        JobHelper.Log(performContext, $"{_messageLogPrefix}{info}");
    }

    /// <summary>
    ///     Logs a warning message to the Hangfire console.
    /// </summary>
    protected void NotifyWarn(PerformContext? performContext, string info)
    {
        if (performContext is not null) performContext.SetTextColor(ConsoleTextColor.Yellow);

        JobHelper.Info(performContext, $"{_messageLogPrefix}{info}");
    }

    /// <summary>
    ///     Logs an error but allows the job to continue processing.
    /// </summary>
    protected void NotifyErrorAndContinue(PerformContext? performContext, string errorMessage)
    {
        JobHelper.Error(performContext, $"{_messageLogPrefix}{errorMessage}");
    }

    /// <summary>
    ///     Logs an error and returns a failed Result to stop the job.
    ///     This is a business error — the job will NOT be retried.
    /// </summary>
    protected Result NotifyErrorAndStop(PerformContext? performContext, string errorMessage)
    {
        JobHelper.Error(performContext, $"{_messageLogPrefix}{errorMessage}");
        return Result.Fail(errorMessage);
    }
}