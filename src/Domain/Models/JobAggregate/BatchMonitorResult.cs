namespace Domain.Models.JobAggregate;

/// <summary>
///     Monitoring result for a batch, combining Hangfire Pro batch state
///     with custom metadata (timing, progress) stored during execution.
/// </summary>
public record BatchMonitorResult
{
    /// <summary>
    ///     The Hangfire Pro batch ID.
    /// </summary>
    public string BatchId { get; init; } = string.Empty;

    /// <summary>
    ///     Human-readable batch name.
    /// </summary>
    public string BatchName { get; init; } = string.Empty;

    /// <summary>
    ///     Current batch status (e.g., "Started", "Succeeded", "Completed", "Cancelled").
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    ///     Total number of jobs in the batch.
    /// </summary>
    public int TotalJobs { get; init; }

    /// <summary>
    ///     Number of jobs that completed successfully.
    /// </summary>
    public int SucceededJobs { get; init; }

    /// <summary>
    ///     Number of jobs that failed.
    /// </summary>
    public int FailedJobs { get; init; }

    /// <summary>
    ///     Number of jobs still pending or processing.
    /// </summary>
    public int PendingJobs { get; init; }

    /// <summary>
    ///     Progress percentage (0.0 to 100.0).
    /// </summary>
    public double PercentageComplete { get; init; }

    /// <summary>
    ///     UTC timestamp when the batch was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    ///     UTC timestamp when all batch jobs finished (null if still running).
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    ///     Total elapsed time from creation to completion (null if still running).
    /// </summary>
    public TimeSpan? ElapsedTime { get; init; }

    /// <summary>
    ///     Formatted elapsed time string for display (e.g., "00:02:34.567").
    /// </summary>
    public string ElapsedTimeFormatted { get; init; } = string.Empty;
}