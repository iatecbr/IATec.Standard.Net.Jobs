using System.Globalization;
using Domain.Contracts.Helpers;
using Domain.Contracts.Services;
using Domain.Models.JobAggregate;
using Hangfire;
using HangFire.Jobs.Commands;
using HangFire.Jobs.Extensions;
using Microsoft.Extensions.Logging;

namespace HangFire.Jobs.Services;

/// <summary>
///     Implementation of <see cref="IBatchJobService" /> using Hangfire Pro Batches.
///     Creates monitored batches with a <see cref="MonitorBatchCommand" /> that tracks progress
///     in real-time via polling and displays a Hangfire Console progress bar.
///     Batch metadata (name, creation time, total jobs, batch key) is stored in a Redis hash
///     with the key pattern "batch:monitor:{batchId}" so the monitor command can read it.
/// </summary>
public class BatchJobService(
    IBackgroundJobClient backgroundJobClient,
    IJobHelper jobHelper,
    ILogger<BatchJobService> logger) : IBatchJobService
{
    /// <inheritdoc />
    public BatchInfo StartMonitoredBatch(string batchName, Action<object, string> jobActions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchName);
        ArgumentNullException.ThrowIfNull(jobActions);

        var createdAt = DateTime.UtcNow;
        var totalJobs = 0;

        // Create a batch key for progress tracking
        var batchKey = BatchKey.CreateNew();

        // Create the batch using Hangfire Pro API
        var batchId = BatchJob.StartNew(batch =>
        {
            // Wrap the user's action with a counter
            var countingAction = new JobCountingBatchAction(batch, () => totalJobs++);
            jobActions(countingAction, batchKey.Value);
        });

        // Initialize batch progress tracking in Redis
        jobHelper.InitializeBatchProgress(batchKey, totalJobs);

        // Store monitoring metadata in Redis (includes BatchKeyValue for progress reads)
        StoreMetadata(batchId, batchName, createdAt, totalJobs, batchKey.Value);

        // Enqueue a standalone monitor command that polls batch progress and shows a real-time progress bar
        backgroundJobClient.EnqueueCommand(new MonitorBatchCommand
        {
            BatchId = batchId,
            BatchName = batchName,
            BatchKeyValue = batchKey.Value
        });

        logger.LogInformation(
            "Monitored batch created: BatchId={BatchId}, BatchName={BatchName}, TotalJobs={TotalJobs}, BatchKey={BatchKey}",
            batchId, batchName, totalJobs, batchKey.Value);

        return new BatchInfo
        {
            BatchId = batchId,
            BatchName = batchName,
            BatchKeyValue = batchKey.Value,
            CreatedAt = createdAt
        };
    }

    /// <inheritdoc />
    public void AttachToBatch(string batchId, Action<object> jobActions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);
        ArgumentNullException.ThrowIfNull(jobActions);

        var additionalJobs = 0;

        BatchJob.Attach(batchId, batch =>
        {
            var countingAction = new JobCountingBatchAction(batch, () => additionalJobs++);
            jobActions(countingAction);
        });

        // Update total job count in metadata
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var metadataKey = $"batch:monitor:{batchId}";
            var existingTotal = connection.GetAllEntriesFromHash(metadataKey)
                ?.GetValueOrDefault("TotalJobs", "0");

            var newTotal = int.Parse(existingTotal ?? "0") + additionalJobs;
            connection.SetRangeInHash(metadataKey, new Dictionary<string, string>
            {
                { "TotalJobs", newTotal.ToString() }
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update batch metadata for {BatchId} after attaching {Count} jobs", batchId,
                additionalJobs);
        }

        logger.LogInformation("Attached {Count} jobs to batch {BatchId}", additionalJobs, batchId);
    }

    /// <inheritdoc />
    public void CancelBatch(string batchId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);

        BatchJob.Cancel(batchId);

        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var metadataKey = $"batch:monitor:{batchId}";
            connection.SetRangeInHash(metadataKey, new Dictionary<string, string>
            {
                { "Status", "Cancelled" },
                { "CancelledAt", DateTime.UtcNow.ToString("O") }
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update batch metadata after cancelling {BatchId}", batchId);
        }

        logger.LogInformation("Batch {BatchId} cancelled", batchId);
    }

    /// <inheritdoc />
    public BatchMonitorResult? GetBatchMonitorResult(string batchId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);

        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var metadataKey = $"batch:monitor:{batchId}";
            var metadata = connection.GetAllEntriesFromHash(metadataKey);

            if (metadata is null || metadata.Count == 0)
                return null;

            var createdAt = DateTime.Parse(
                metadata.GetValueOrDefault("CreatedAt", DateTime.UtcNow.ToString("O")),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
            var totalJobs = int.Parse(metadata.GetValueOrDefault("TotalJobs", "0"));
            var status = metadata.GetValueOrDefault("Status", "Processing");

            DateTime? completedAt = metadata.TryGetValue("CompletedAt", out var completedAtStr)
                ? DateTime.Parse(completedAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                : null;

            TimeSpan? elapsed = metadata.TryGetValue("ElapsedMs", out var elapsedMs)
                ? TimeSpan.FromMilliseconds(double.Parse(elapsedMs))
                : null;

            // If still running, calculate elapsed from now
            elapsed ??= status != "Cancelled" ? DateTime.UtcNow - createdAt : null;

            // Read progress from the batch progress tracker (separate Redis hash)
            var succeededJobs = 0;
            var failedJobs = 0;
            var pendingJobs = totalJobs;
            var percentageComplete = 0.0;

            if (metadata.TryGetValue("BatchKeyValue", out var batchKeyValue)
                && !string.IsNullOrEmpty(batchKeyValue))
            {
                var progressHash = connection.GetAllEntriesFromHash(batchKeyValue);
                if (progressHash is not null && progressHash.Count > 0)
                {
                    succeededJobs = int.Parse(progressHash.GetValueOrDefault("Completed", "0"));
                    failedJobs = int.Parse(progressHash.GetValueOrDefault("Failed", "0"));
                    var totalProcessed = succeededJobs + failedJobs;
                    pendingJobs = Math.Max(0, totalJobs - totalProcessed);
                    percentageComplete = totalJobs > 0
                        ? Math.Round(totalProcessed * 100.0 / totalJobs, 1)
                        : 0;
                }
            }

            return new BatchMonitorResult
            {
                BatchId = batchId,
                BatchName = metadata.GetValueOrDefault("BatchName", string.Empty),
                Status = status,
                TotalJobs = totalJobs,
                SucceededJobs = succeededJobs,
                FailedJobs = failedJobs,
                PendingJobs = pendingJobs,
                PercentageComplete = percentageComplete,
                CreatedAt = createdAt,
                CompletedAt = completedAt,
                ElapsedTime = elapsed,
                ElapsedTimeFormatted = elapsed is not null ? FormatElapsed(elapsed.Value) : string.Empty
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read batch monitor result for {BatchId}", batchId);
            return null;
        }
    }

    private static void StoreMetadata(string batchId, string batchName, DateTime createdAt, int totalJobs,
        string batchKeyValue)
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var metadataKey = $"batch:monitor:{batchId}";

            connection.SetRangeInHash(metadataKey, new Dictionary<string, string>
            {
                { "BatchId", batchId },
                { "BatchName", batchName },
                { "CreatedAt", createdAt.ToString("O") },
                { "TotalJobs", totalJobs.ToString() },
                { "BatchKeyValue", batchKeyValue },
                { "Status", "Started" }
            });
        }
        catch
        {
            // Metadata storage failure should not prevent batch creation
        }
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return $"{elapsed:hh\\:mm\\:ss\\.fff}";

        if (elapsed.TotalMinutes >= 1)
            return $"{elapsed:mm\\:ss\\.fff}";

        return $"{elapsed:ss\\.fff}s";
    }
}

/// <summary>
///     Internal wrapper that wraps an <see cref="IBatchAction" /> and counts how many jobs are enqueued.
///     This is passed to user code so they can enqueue jobs while we track the count.
///     The user code casts this to the expected batch action type — since the Hangfire Pro API
///     uses <see cref="IBatchAction" /> internally, this acts as a transparent proxy.
/// </summary>
internal sealed class JobCountingBatchAction(IBatchAction inner, Action incrementCounter)
{
    /// <summary>
    ///     The underlying Hangfire Pro batch action.
    ///     User code should cast the <c>object</c> parameter to this type
    ///     or use the extension methods in <see cref="Extensions.BatchJobServiceExtensions" />.
    /// </summary>
    public IBatchAction Inner { get; } = inner;

    /// <summary>
    ///     Increments the internal job counter.
    /// </summary>
    public void IncrementCount()
    {
        incrementCounter();
    }
}
