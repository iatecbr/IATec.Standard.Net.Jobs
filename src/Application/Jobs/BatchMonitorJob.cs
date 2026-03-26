using System.Globalization;
using Application.Base;
using Application.Constants;
using Domain.Contracts.Helpers;
using FluentResults;
using Hangfire;
using Hangfire.Server;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;

namespace Application.Jobs;

/// <summary>
///     Generic monitor job that tracks batch progress in real-time via a Hangfire Console progress bar.
///     Enqueued as a standalone job (not a continuation) alongside the batch — it polls the
///     batch progress Redis hash until all jobs complete, updating the progress bar on each tick.
///     When done, records total execution time and final status.
///     This job is automatically enqueued by <see cref="Services.BatchJobService" />.
///     It should NOT be enqueued manually.
/// </summary>
[AutomaticRetry(Attempts = JobRetryPolicyConstant.NoRetry)]
[Queue(JobRetryPolicyConstant.DefaultQueue)]
public class BatchMonitorJob(
    IJobHelper jobHelper,
    ILogger<BatchMonitorJob> logger) : BaseJob(jobHelper, logger, "[BatchMonitor] ")
{
    private const int PollingIntervalMs = 500;

    // Stored by the Hangfire entry point before the base lifecycle runs.
    private string _batchId = string.Empty;
    private string _batchKeyValue = string.Empty;
    private string _batchName = string.Empty;

    /// <summary>
    ///     Entry point called by Hangfire.
    ///     Stores the batch parameters, then delegates to the base class lifecycle
    ///     (Start -> RunAsync -> Finally).
    /// </summary>
    /// <param name="batchId">The Hangfire Pro batch ID being monitored.</param>
    /// <param name="batchName">Human-readable name of the batch.</param>
    /// <param name="batchKeyValue">The batch progress Redis key for reading completed/failed counts.</param>
    /// <param name="performContext">Hangfire performance context (injected automatically).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [JobDisplayName("Monitor Batch: {1}")]
    public Task ExecuteAsync(
        string batchId,
        string batchName,
        string batchKeyValue,
        PerformContext? performContext,
        CancellationToken cancellationToken)
    {
        _batchId = batchId;
        _batchName = batchName;
        _batchKeyValue = batchKeyValue;
        return base.ExecuteAsync(performContext, cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task<Result> RunAsync(PerformContext? performContext, CancellationToken cancellationToken)
    {
        var metadataKey = $"batch:monitor:{_batchId}";

        IStorageConnection? connection = null;
        Dictionary<string, string>? metadata;

        try
        {
            connection = JobStorage.Current.GetConnection();
            metadata = connection.GetAllEntriesFromHash(metadataKey);
        }
        catch (Exception ex)
        {
            connection?.Dispose();
            logger.LogWarning(ex, "Failed to read batch metadata for {BatchId}. Monitor job will exit gracefully.",
                _batchId);
            NotifyWarn(performContext,
                $"Failed to read metadata for batch '{_batchName}' ({_batchId}). Storage error: {ex.Message}");
            return Result.Ok();
        }

        using (connection)
        {
            if (metadata is null || metadata.Count == 0)
            {
                NotifyWarn(performContext,
                    $"No metadata found for batch '{_batchName}' ({_batchId}).");
                return Result.Ok();
            }

            var createdAt = DateTime.Parse(
                metadata.GetValueOrDefault("CreatedAt", DateTime.UtcNow.ToString("O")),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
            var totalJobs = int.Parse(
                metadata.GetValueOrDefault("TotalJobs", "0"));

            if (totalJobs == 0)
            {
                NotifyWarn(performContext, $"Batch '{_batchName}' has 0 jobs.");
                return Result.Ok();
            }

            NotifyInfo(performContext, $"Monitoring batch '{_batchName}' | {totalJobs} jobs | ID: {_batchId}");

            // Poll progress until all jobs are done
            var lastPercentage = -1;
            while (!cancellationToken.IsCancellationRequested)
            {
                var (completed, failed, total) = ReadProgress(connection);

                var totalProcessed = completed + failed;
                var percentage = Math.Min(100, total > 0 ? (int)(totalProcessed * 100.0 / total) : 0);

                if (percentage != lastPercentage)
                {
                    JobHelper.ProgressBar(performContext, percentage);
                    lastPercentage = percentage;
                }

                if (totalProcessed >= total)
                    break;

                await Task.Delay(PollingIntervalMs, cancellationToken);
            }

            // Record completion
            var completedAt = DateTime.UtcNow;
            var elapsed = completedAt - createdAt;
            var (finalCompleted, finalFailed, _) = ReadProgress(connection);

            connection.SetRangeInHash(metadataKey, new Dictionary<string, string>
            {
                { "CompletedAt", completedAt.ToString("O") },
                { "ElapsedMs", elapsed.TotalMilliseconds.ToString("F0") },
                { "Status", "Completed" }
            });

            // Ensure progress bar shows 100%
            JobHelper.ProgressBar(performContext, 100);

            var summary = $"Batch '{_batchName}' completed" +
                          $" | Succeeded: {finalCompleted}" +
                          $" | Failed: {finalFailed}" +
                          $" | Total: {totalJobs}" +
                          $" | Elapsed: {FormatElapsed(elapsed)}";

            NotifyInfo(performContext, summary);

            logger.LogInformation(
                "Batch monitor completed: BatchId={BatchId}, BatchName={BatchName}, Succeeded={Succeeded}, Failed={Failed}, TotalJobs={TotalJobs}, Elapsed={Elapsed}",
                _batchId, _batchName, finalCompleted, finalFailed, totalJobs, FormatElapsed(elapsed));

            return Result.Ok();
        }
    }

    private (int Completed, int Failed, int Total) ReadProgress(IStorageConnection connection)
    {
        try
        {
            var hash = connection.GetAllEntriesFromHash(_batchKeyValue);
            if (hash is null || hash.Count == 0)
                return (0, 0, 0);

            var total = int.Parse(hash.GetValueOrDefault("Total", "0"));
            var completed = int.Parse(hash.GetValueOrDefault("Completed", "0"));
            var failed = int.Parse(hash.GetValueOrDefault("Failed", "0"));

            return (completed, failed, total);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read batch progress for {BatchKey}", _batchKeyValue);
            return (0, 0, 0);
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