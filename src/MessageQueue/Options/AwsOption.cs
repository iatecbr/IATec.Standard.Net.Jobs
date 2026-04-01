namespace MessageQueue.Options;

/// <summary>
///     Top-level AWS configuration. Bound from "AWS" section in appsettings.json.
///     Contains nested SQS options and an optional ServiceUrl for LocalStack.
/// </summary>
public class AwsOption
{
    public const string Key = "AWS";

    /// <summary>
    ///     Optional service URL override for local development (e.g., LocalStack).
    ///     When set, both SQS and SNS clients will use this endpoint.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    ///     Amazon SQS specific configuration.
    /// </summary>
    public SqsOption? Sqs { get; set; }
}