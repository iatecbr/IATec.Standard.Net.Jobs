namespace Domain.Options;

/// <summary>
///     Amazon SQS specific configuration. Nested under AwsOption.
/// </summary>
public class SqsOption //TODO: move to library
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;

    /// <summary>
    ///     Queue name prefix/scope for environment isolation (e.g., "dev", "staging").
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    ///     Number of retry attempts for message processing.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    ///     Interval in milliseconds between retry attempts.
    /// </summary>
    public int IntervalMilliSeconds { get; set; }
}