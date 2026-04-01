namespace HangFire.Jobs.Constants;

/// <summary>
///     Centralized retry policy constants for Hangfire jobs.
/// </summary>
public static class JobRetryPolicyConstant
{
    /// <summary>
    ///     Default number of automatic retry attempts for jobs.
    /// </summary>
    public const int DefaultRetryAttempts = 3;

    /// <summary>
    ///     Retry attempts for jobs that should NOT be retried (business errors).
    ///     Used with [AutomaticRetry(Attempts = NoRetry)] on jobs that return Result.Fail() via FluentResults.
    /// </summary>
    public const int NoRetry = 0;

    /// <summary>
    ///     High retry count for critical jobs that must eventually succeed.
    /// </summary>
    public const int HighRetryAttempts = 10;

    /// <summary>
    ///     Default Hangfire queue name.
    /// </summary>
    public const string DefaultQueue = "default";

    /// <summary>
    ///     Queue name for long-running or resource-intensive jobs.
    /// </summary>
    public const string HeavyQueue = "heavy";
}
