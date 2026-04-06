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
///     All properties are positional constructor parameters so that Newtonsoft.Json (used by
///     Hangfire for serialization) can correctly round-trip the record through its constructor.
/// </summary>
[AutomaticRetry(Attempts = JobRetryPolicyConstant.DefaultRetryAttempts)]
[Queue(JobRetryPolicyConstant.DefaultQueue)]
[CommandDisplayName("Process Asset: {0}")]
public sealed record ProcessAssetCommand(
    Guid AssetId,
    string Code,
    string Name,
    decimal Value,
    string? BatchKeyValue = null) : IRequest<Result>
{
    public override string ToString()
    {
        return $"{Code} - {Name}";
    }
}