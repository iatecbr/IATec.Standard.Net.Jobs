namespace Domain.Helpers;

/// <summary>
///     Represents progress information for a batch of jobs.
/// </summary>
public record BatchProgressInfo
{
    public string BatchKey { get; init; } = string.Empty;
    public int Total { get; init; }
    public int Completed { get; init; }
    public int Failed { get; init; }
    public int Pending { get; init; }
    public double PercentageComplete { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? LastUpdated { get; init; }
    public string Status { get; init; } = "Processing";
}