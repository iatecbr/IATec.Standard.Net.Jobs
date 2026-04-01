using Hangfire.Server;
using HangFire.Jobs.Contracts;

namespace HangFire.Jobs.Helpers;

/// <summary>
///     Stores the current <see cref="PerformContext" /> in an <see cref="AsyncLocal{T}" />
///     so it flows through async calls within the same Hangfire job execution.
///     Set by <see cref="Filters.CommandAttributeJobFilter.OnPerforming" />,
///     consumed by <see cref="Base.BaseCommand{TCommand}" /> and any service that needs Hangfire Console access.
/// </summary>
public sealed class PerformContextAccessor : IPerformContextAccessor
{
    private static readonly AsyncLocal<PerformContext?> CurrentContext = new();

    public PerformContext? PerformContext
    {
        get => CurrentContext.Value;
        set => CurrentContext.Value = value;
    }

    /// <summary>
    ///     Sets the <see cref="PerformContext" /> for the current async scope.
    ///     Called by <see cref="Filters.CommandAttributeJobFilter" /> which runs outside DI
    ///     as a global Hangfire filter.
    /// </summary>
    internal static void Set(PerformContext? context) => CurrentContext.Value = context;

    /// <summary>
    ///     Clears the <see cref="PerformContext" /> for the current async scope.
    /// </summary>
    internal static void Clear() => CurrentContext.Value = null;
}
