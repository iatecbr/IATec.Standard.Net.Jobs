using Domain.Helpers;

namespace Domain.Contracts.Helpers;

/// <summary>
///     Abstraction for job execution helper methods.
///     Provides logging, progress tracking, and batch management for Hangfire jobs.
///     The implementation (in Application layer) wraps Hangfire-specific types like PerformContext.
/// </summary>
public interface IJobHelper
{
    /// <summary>
    ///     Signals that a job has started. Writes initial log entry.
    /// </summary>
    void Start(object? performContext);

    /// <summary>
    ///     Signals that a job has completed successfully.
    /// </summary>
    void Finish(object? performContext);

    /// <summary>
    ///     Logs an error and cleans up progress bar state.
    /// </summary>
    void Error(object? performContext, Exception? exception = null);

    /// <summary>
    ///     Logs an error message and cleans up progress bar state.
    /// </summary>
    void Error(object? performContext, string error);

    /// <summary>
    ///     Performs cleanup of progress bars. Called in finally blocks.
    /// </summary>
    void Finally(object? performContext);

    /// <summary>
    ///     Writes an informational message to the job console.
    /// </summary>
    void Info(object? performContext, string info);

    /// <summary>
    ///     Writes a log entry without console output.
    /// </summary>
    void Log(object? performContext, string info);

    /// <summary>
    ///     Sets the progress bar to the specified percentage.
    /// </summary>
    void ProgressBar(object? performContext, int percentage);

    /// <summary>
    ///     Updates progress based on current/total item count.
    /// </summary>
    void UpdateProgress(object? performContext, int current, int total);

    /// <summary>
    ///     Updates progress with a custom message.
    /// </summary>
    void UpdateProgressWithMessage(object? performContext, int current, int total, string message);

    /// <summary>
    ///     Creates a unique batch key for tracking grouped job progress.
    /// </summary>
    BatchKey CreateBatchKey(object? performContext);

    /// <summary>
    ///     Initializes batch progress tracking in Hangfire storage.
    /// </summary>
    void InitializeBatchProgress(BatchKey batchKey, int totalItems);

    /// <summary>
    ///     Increments the completed (or failed) counter for a batch.
    /// </summary>
    void IncrementBatchProgress(BatchKey batchKey, object? performContext, bool failed = false);

    /// <summary>
    ///     Retrieves current progress information for a batch.
    /// </summary>
    BatchProgressInfo? GetBatchProgress(BatchKey batchKey);

    /// <summary>
    ///     Awaits all tasks with periodic progress reporting.
    ///     Unlike the 7You/Team reference, this is properly async (no .Wait() deadlocks).
    /// </summary>
    Task WhenAllEx(ICollection<Task> tasks, Action<ICollection<Task>> reportProgressAction);
}