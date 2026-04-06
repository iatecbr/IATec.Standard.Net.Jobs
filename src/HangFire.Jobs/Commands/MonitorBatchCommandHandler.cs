using System.Globalization;
using Domain.Contracts.Helpers;
using FluentResults;
using Hangfire;
using HangFire.Jobs.Base;
using HangFire.Jobs.Contracts;
using Hangfire.Storage;

namespace HangFire.Jobs.Commands;

/// <summary>
///     Handler for <see cref="MonitorBatchCommand" />.
///     Generic monitor that tracks any batch's progress in real-time via a Hangfire Console progress bar.
///     Polls the batch progress Redis hash until all jobs complete, updating the progress bar on each tick.
///     When done, records total execution time and final status in the batch metadata hash.
///     Works with any command type enqueued inside a monitored batch — the monitor is completely
///     agnostic to the type of jobs being executed. It only reads progress counters from Redis.
/// </summary>
public sealed class MonitorBatchCommandHandler(
    IJobHelper jobHelper,
    IPerformContextAccessor performContextAccessor)
    : BaseCommand<MonitorBatchCommand>(jobHelper, performContextAccessor, "[BatchMonitor] ")
{
    private const int PollingIntervalMs = 500;
    private const int MaxMonitorDurationMinutes = 30;

    /// <inheritdoc />
    protected override async Task<Result> HandleAsync(
        MonitorBatchCommand request, CancellationToken cancellationToken)
    {
        var metadataKey = $"batch:monitor:{request.BatchId}";

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
            NotifyWarn(
                $"Failed to read metadata for batch '{request.BatchName}' ({request.BatchId}). Storage error: {ex.Message}");
            return Result.Ok();
        }

        using (connection)
        {
            if (metadata is null || metadata.Count == 0)
            {
                NotifyWarn(
                    $"No metadata found for batch '{request.BatchName}' ({request.BatchId}).");
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
                NotifyWarn($"Batch '{request.BatchName}' has 0 jobs.");
                return Result.Ok();
            }

            NotifyInfo(
                $"Monitoring batch '{request.BatchName}' | {totalJobs} jobs | ID: {request.BatchId}");

            // Poll progress until all jobs are done or timeout is reached
            var lastPercentage = -1;
            var deadline = DateTime.UtcNow.AddMinutes(MaxMonitorDurationMinutes);

            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
            {
                var (completed, failed, total) = ReadProgress(connection, request.BatchKeyValue);

                var totalProcessed = completed + failed;
                var percentage = Math.Min(100,
                    total > 0 ? (int)(totalProcessed * 100.0 / total) : 0);

                if (percentage != lastPercentage)
                {
                    JobHelper.ProgressBar(PerformContext, percentage);
                    lastPercentage = percentage;
                }

                if (totalProcessed >= total)
                    break;

                await Task.Delay(PollingIntervalMs, cancellationToken);
            }

            if (DateTime.UtcNow >= deadline)
            {
                NotifyWarn(
                    $"Batch '{request.BatchName}' monitor timed out after {MaxMonitorDurationMinutes} minutes.");
            }

            // Record completion
            var completedAt = DateTime.UtcNow;
            var elapsed = completedAt - createdAt;
            var (finalCompleted, finalFailed, _) = ReadProgress(connection, request.BatchKeyValue);

            connection.SetRangeInHash(metadataKey, new Dictionary<string, string>
            {
                { "CompletedAt", completedAt.ToString("O") },
                { "ElapsedMs", elapsed.TotalMilliseconds.ToString("F0") },
                { "Status", "Completed" }
            });

            // Ensure progress bar shows 100%
            JobHelper.ProgressBar(PerformContext, 100);

            var summary = $"Batch '{request.BatchName}' completed" +
                          $" | Succeeded: {finalCompleted}" +
                          $" | Failed: {finalFailed}" +
                          $" | Total: {totalJobs}" +
                          $" | Elapsed: {FormatElapsed(elapsed)}";

            NotifyInfo(summary);

            return Result.Ok();
        }
    }

    private (int Completed, int Failed, int Total) ReadProgress(
        IStorageConnection connection, string batchKeyValue)
    {
        try
        {
            var hash = connection.GetAllEntriesFromHash(batchKeyValue);
            if (hash is null || hash.Count == 0)
                return (0, 0, 0);

            var total = int.Parse(hash.GetValueOrDefault("Total", "0"));
            var completed = int.Parse(hash.GetValueOrDefault("Completed", "0"));
            var failed = int.Parse(hash.GetValueOrDefault("Failed", "0"));

            return (completed, failed, total);
        }
        catch (Exception ex)
        {
            NotifyWarn($"Failed to read batch progress for {batchKeyValue}: {ex.Message}");
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