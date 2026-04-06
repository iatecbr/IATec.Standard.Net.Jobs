using Domain.Helpers;

namespace Domain.Contracts.Services;

/// <summary>
///     Abstraction for creating and managing Hangfire Pro batch jobs.
///     Decouples the Domain from Hangfire-specific types (BatchJob, IBatchAction).
/// </summary>
public interface IBatchJobService
{
    /// <summary>
    ///     Creates a new batch of background jobs with an automatic monitor job.
    ///     The monitor job is enqueued as a standalone polling job that tracks progress
    ///     in real-time via Redis and displays a Hangfire Console progress bar.
    /// </summary>
    /// <param name="batchName">Human-readable name for the batch (displayed in dashboard and logs).</param>
    /// <param name="jobActions">
    ///     Action that receives a batch context and the batch key value.
    ///     The batch context is typed as <c>object</c> to avoid Hangfire dependency in Domain;
    ///     the implementation wraps <c>IBatchAction</c>.
    ///     The batch key value (string) should be passed to each child job's data DTO
    ///     so they can report progress via <see cref="Domain.Contracts.Helpers.IJobHelper.IncrementBatchProgress" />.
    /// </param>
    /// <returns>Batch metadata including the batch ID and monitoring key.</returns>
    BatchInfo StartMonitoredBatch(string batchName, Action<object, string> jobActions);

    /// <summary>
    ///     Attaches additional jobs to an existing batch.
    ///     If the batch was already completed, it moves back to the started state.
    /// </summary>
    /// <param name="batchId">The Hangfire Pro batch ID.</param>
    /// <param name="jobActions">Action to enqueue additional jobs into the batch.</param>
    void AttachToBatch(string batchId, Action<object> jobActions);

    /// <summary>
    ///     Cancels a batch, preventing pending jobs from being executed.
    ///     Jobs already in progress will complete, but queued jobs will be deleted.
    /// </summary>
    /// <param name="batchId">The Hangfire Pro batch ID.</param>
    void CancelBatch(string batchId);

    /// <summary>
    ///     Retrieves the current monitoring information for a batch.
    ///     Includes progress, timing, and status from both Hangfire and stored metadata.
    /// </summary>
    /// <param name="batchId">The Hangfire Pro batch ID.</param>
    /// <returns>Batch monitoring information, or null if the batch is not found.</returns>
    BatchMonitorResult? GetBatchMonitorResult(string batchId);
}