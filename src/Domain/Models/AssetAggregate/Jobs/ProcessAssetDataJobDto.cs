namespace Domain.Models.AssetAggregate.Jobs;

public record ProcessAssetDataJobDto
{
    public Guid AssetId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal Value { get; init; }

    /// <summary>
    ///     Optional batch key for progress tracking.
    ///     When this job is part of a monitored batch, the batch key is set
    ///     so the job can report its completion back to the batch progress tracker.
    ///     Null when running as a standalone job.
    /// </summary>
    public string? BatchKeyValue { get; init; }
}