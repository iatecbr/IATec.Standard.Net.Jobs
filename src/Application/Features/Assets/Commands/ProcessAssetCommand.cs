using FluentResults;
using Hangfire;
using HangFire.Jobs.Constants;
using HangFire.Jobs.Filters;
using MediatR;

namespace Application.Features.Assets.Commands;

/// <summary>
///     Command to process a single asset.
///     Hangfire attributes are defined here on the command record.
///     When enqueued via <c>backgroundJobClient.EnqueueCommand(command)</c>,
///     the <see cref="CommandAttributeJobFilter" /> propagates these attributes to the Hangfire job.
///     Validation runs inside the Hangfire worker via <c>ValidatorPipelineBehavior</c>.
/// </summary>
[AutomaticRetry(Attempts = JobRetryPolicyConstant.DefaultRetryAttempts)]
[Queue(JobRetryPolicyConstant.DefaultQueue)]
[CommandDisplayName("Process Asset: {0}")]
public sealed record ProcessAssetCommand : IRequest<Result>
{
    public Guid AssetId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal Value { get; init; }

    /// <summary>
    ///     Optional batch key for progress tracking.
    ///     When this command runs as part of a monitored batch, the batch key is set
    ///     so the handler can report completion back to the batch progress tracker.
    ///     Null when running as a standalone job.
    /// </summary>
    public string? BatchKeyValue { get; init; }

    public override string ToString() => $"{Code} - {Name}";
}
