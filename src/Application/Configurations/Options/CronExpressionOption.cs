namespace Application.Configurations.Options;

public class CronExpressionOption
{
    public const string Key = "CronExpressions";

    public string EveryMinute { get; init; } = string.Empty;
}