using Hangfire.Server;

namespace HangFire.Jobs.Contracts;

/// <summary>
///     Provides access to the current Hangfire <see cref="PerformContext" />
///     for command handlers running inside a Hangfire worker via <c>ISender.Send(command)</c>.
///     The context is captured by <see cref="Filters.CommandAttributeJobFilter" /> in
///     <c>OnPerforming</c> and stored in an <see cref="AsyncLocal{T}" /> so it can be
///     injected into any service resolved within the same async scope.
/// </summary>
public interface IPerformContextAccessor
{
    PerformContext? PerformContext { get; set; }
}
