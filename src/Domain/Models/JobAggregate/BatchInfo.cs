namespace Domain.Models.JobAggregate;

/// <summary>
///     Metadata returned when a monitored batch is created.
///     Contains identifiers needed to track the batch and its monitor job.
/// </summary>
public record BatchInfo
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
    ///     The raw Redis key used for batch progress tracking.
    /// </summary>
    public string BatchKeyValue { get; init; } = string.Empty;

    /// <summary>
    ///     UTC timestamp when the batch was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}