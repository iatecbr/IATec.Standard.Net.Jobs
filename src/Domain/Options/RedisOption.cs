namespace Domain.Options;

public class RedisOption //TODO: move to library
{
    public const string Key = "Redis";
    public string ConnectionString { get; set; } = string.Empty;
    public int Database { get; set; }
}