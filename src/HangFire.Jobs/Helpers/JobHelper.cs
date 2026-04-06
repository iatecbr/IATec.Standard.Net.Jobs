using System.Collections.Concurrent;
using Domain.Contracts.Helpers;
using Domain.Helpers;
using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Progress;
using Hangfire.Server;

namespace HangFire.Jobs.Helpers;

/// <summary>
///     Thread-safe implementation of <see cref="IJobHelper" />.
///     Fixes from 7You/Team reference:
///     - Uses <see cref="ConcurrentDictionary{TKey,TValue}" /> instead of Dictionary + lock for progress bars
///     - All progress bar operations are thread-safe
///     - WhenAllEx is properly async (no .Wait() deadlocks)
/// </summary>
public class JobHelper : IJobHelper
{
    private readonly ConcurrentDictionary<string, IProgressBar> _progressBars = new();

    public void Start(object? performContext)
    {
        var context = CastContext(performContext);
        Writer(context, "Starting job", ConsoleTextColor.Gray);
    }

    public void Finish(object? performContext)
    {
        var context = CastContext(performContext);
        Writer(context, "Job Ran", ConsoleTextColor.Green);
    }

    public void Error(object? performContext, Exception? exception = null)
    {
        var context = CastContext(performContext);

        if (context?.BackgroundJob?.Id is not null) _progressBars.TryRemove(context.BackgroundJob.Id, out _);

        if (exception is not null)
            Serilog.Log.Error(exception, "Job error [{JobType}] [{JobId}]",
                context?.BackgroundJob?.Job?.Type?.Name ?? "Unknown",
                context?.BackgroundJob?.Id ?? "Unknown");

        Writer(context, "Job Error", ConsoleTextColor.Red);
    }

    public void Error(object? performContext, string error)
    {
        var context = CastContext(performContext);

        if (context?.BackgroundJob?.Id is not null) _progressBars.TryRemove(context.BackgroundJob.Id, out _);

        Serilog.Log.Error("Job error [{JobType}] [{JobId}]: {Error}",
            context?.BackgroundJob?.Job?.Type?.Name ?? "Unknown",
            context?.BackgroundJob?.Id ?? "Unknown",
            error);

        Writer(context, "Job Error", ConsoleTextColor.Red);
    }

    public void Finally(object? performContext)
    {
        var context = performContext as PerformContext;

        if (context?.BackgroundJob?.Id is not null) _progressBars.TryRemove(context.BackgroundJob.Id, out _);

        Writer(context, "Job Finished", ConsoleTextColor.DarkGreen);
    }

    public void Info(object? performContext, string info)
    {
        var context = CastContext(performContext);
        Writer(context, info);
    }

    public void Log(object? performContext, string info)
    {
        var context = CastContext(performContext);
        if (context is null) return;

        Serilog.Log.Information("{Info} [Job:{TypeName}] [Id:{BackgroundJobId}]",
            info,
            context.BackgroundJob?.Job?.Type?.Name ?? "Unknown",
            context.BackgroundJob?.Id ?? "Unknown");
    }

    public void ProgressBar(object? performContext, int percentage)
    {
        var context = performContext as PerformContext;
        if (context?.BackgroundJob?.Id is null) return;

        try
        {
            var jobId = context.BackgroundJob.Id;
            var progressBar = _progressBars.GetOrAdd(jobId, _ => context.WriteProgressBar());
            progressBar.SetValue(percentage);
        }
        catch
        {
            // Don't fail the job if progress bar update fails
        }
    }

    public void UpdateProgress(object? performContext, int current, int total)
    {
        var context = performContext as PerformContext;
        if (context is null || total == 0) return;

        try
        {
            var percentage = Math.Min(100, current * 100 / total);
            ProgressBar(context, percentage);

            context.SetJobParameter("Progress", $"{percentage}%");
            context.SetJobParameter("Count", $"{current}/{total}");
        }
        catch
        {
            // Don't fail the job if progress update fails
        }
    }

    public void UpdateProgressWithMessage(object? performContext, int current, int total, string message)
    {
        var context = performContext as PerformContext;
        if (context is null || total == 0) return;

        try
        {
            var percentage = Math.Min(100, current * 100 / total);
            ProgressBar(context, percentage);

            context.SetJobParameter("Progress", $"{percentage}%");
            context.SetJobParameter("Count", $"{current}/{total}");
            context.SetJobParameter("Message", message);
        }
        catch
        {
            // Don't fail the job if progress update fails
        }
    }

    public BatchKey CreateBatchKey(object? performContext)
    {
        var context = performContext as PerformContext;
        var jobId = context?.BackgroundJob?.Id ?? Guid.NewGuid().ToString();
        return BatchKey.FromJobId(jobId);
    }

    public void InitializeBatchProgress(BatchKey batchKey, int totalItems)
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            connection.SetRangeInHash(batchKey.Value, new Dictionary<string, string>
            {
                { "Total", totalItems.ToString() },
                { "Completed", "0" },
                { "Failed", "0" },
                { "StartedAt", DateTime.UtcNow.ToString("O") }
            });
        }
        catch
        {
            // Don't fail if batch initialization fails
        }
    }

    public void IncrementBatchProgress(BatchKey batchKey, object? performContext, bool failed = false)
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();

            // Distributed lock prevents concurrent workers from losing increments
            // during the read-modify-write cycle on the batch progress hash.
            var lockKey = $"batch-progress-lock:{batchKey.Value}";
            using (connection.AcquireDistributedLock(lockKey, TimeSpan.FromSeconds(30)))
            {
                var hash = connection.GetAllEntriesFromHash(batchKey.Value);

                if (hash is null || hash.Count == 0) return;

                var total = int.Parse(hash.GetValueOrDefault("Total", "0"));
                var completed = int.Parse(hash.GetValueOrDefault("Completed", "0"));
                var failedCount = int.Parse(hash.GetValueOrDefault("Failed", "0"));

                if (failed)
                    failedCount++;
                else
                    completed++;

                var totalProcessed = completed + failedCount;
                var percentage = total > 0 ? totalProcessed * 100.0 / total : 0;

                var updateData = new Dictionary<string, string>
                {
                    { "Completed", completed.ToString() },
                    { "Failed", failedCount.ToString() },
                    { "LastUpdated", DateTime.UtcNow.ToString("O") }
                };

                if (totalProcessed >= total)
                {
                    updateData.Add("CompletedAt", DateTime.UtcNow.ToString("O"));
                    updateData.Add("Status", "Completed");
                }

                connection.SetRangeInHash(batchKey.Value, updateData);

                if (performContext is PerformContext context)
                    context.SetJobParameter("BatchProgress",
                        $"{totalProcessed}/{total} ({percentage:F1}%)");
            }
        }
        catch
        {
            // Don't fail the job if batch progress update fails
        }
    }

    public BatchProgressInfo? GetBatchProgress(BatchKey batchKey)
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var hash = connection.GetAllEntriesFromHash(batchKey.Value);

            if (hash is null || hash.Count == 0)
                return null;

            var total = int.Parse(hash.GetValueOrDefault("Total", "0"));
            var completed = int.Parse(hash.GetValueOrDefault("Completed", "0"));
            var failedCount = int.Parse(hash.GetValueOrDefault("Failed", "0"));
            var totalProcessed = completed + failedCount;

            return new BatchProgressInfo
            {
                BatchKey = batchKey.Value,
                Total = total,
                Completed = completed,
                Failed = failedCount,
                Pending = total - totalProcessed,
                PercentageComplete = total > 0 ? totalProcessed * 100.0 / total : 0,
                StartedAt = DateTime.Parse(hash.GetValueOrDefault("StartedAt", DateTime.UtcNow.ToString("O"))),
                CompletedAt = hash.TryGetValue("CompletedAt", out var completedAt)
                    ? DateTime.Parse(completedAt)
                    : null,
                LastUpdated = hash.TryGetValue("LastUpdated", out var lastUpdated)
                    ? DateTime.Parse(lastUpdated)
                    : null,
                Status = hash.GetValueOrDefault("Status", "Processing")
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Awaits all tasks with periodic progress reporting.
    ///     Properly async — no .Wait() that could cause deadlocks.
    ///     Uses ConcurrentBag for thread-safe status collection.
    /// </summary>
    public async Task WhenAllEx(ICollection<Task> tasks, Action<ICollection<Task>> reportProgressAction)
    {
        var whenAllTask = Task.WhenAll(tasks);

        while (true)
        {
            var timer = Task.Delay(250);
            await Task.WhenAny(whenAllTask, timer);

            if (whenAllTask.IsCompleted)
            {
                if (!whenAllTask.IsFaulted) return;

                if (whenAllTask.Exception?.InnerException is not null)
                    throw whenAllTask.Exception.InnerException;

                throw new InvalidOperationException("Unknown error during parallel task execution");
            }

            reportProgressAction(tasks);
        }
    }

    private static PerformContext? CastContext(object? performContext)
    {
        return performContext as PerformContext;
    }

    private static void Writer(PerformContext? context, string info, ConsoleTextColor? color = null)
    {
        if (context is null) return;

        if (color is not null) context.SetTextColor(color);

        Serilog.Log.Information("\n\n############ {Info} [Job:{TypeName}] [Id:{BackgroundJobId}] ############\n\n",
            info,
            context.BackgroundJob?.Job?.Type?.Name ?? "Unknown",
            context.BackgroundJob?.Id ?? "Unknown");

        context.WriteLine($"{info} {DateTime.UtcNow}");
    }
}