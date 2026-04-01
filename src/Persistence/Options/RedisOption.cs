namespace Persistence.Options;

public class RedisOption
{
    public const string Key = "ConnectionStrings";
    private const string RedisConnectionKey = "RedisConnection";
    private const int DefaultDatabase = 0;

    private string _connectionString = string.Empty;
    private int? _parsedDatabase;

    public string ConnectionString
    {
        get => _connectionString;
        set
        {
            _connectionString = value;
            ParseDatabase();
        }
    }

    public int Database => _parsedDatabase ?? DefaultDatabase;

    /// <summary>
    ///     Returns the connection string without the database suffix (e.g., "localhost:6379").
    ///     Used by StackExchange.Redis ConnectionMultiplexer which handles defaultDatabase separately.
    /// </summary>
    public string HostConnectionString
    {
        get
        {
            var slashIndex = _connectionString.LastIndexOf('/');
            return slashIndex > 0 && _parsedDatabase.HasValue
                ? _connectionString[..slashIndex]
                : _connectionString;
        }
    }

    private void ParseDatabase()
    {
        _parsedDatabase = null;

        if (string.IsNullOrEmpty(_connectionString))
            return;

        var slashIndex = _connectionString.LastIndexOf('/');
        if (slashIndex < 0 || slashIndex >= _connectionString.Length - 1)
            return;

        var databasePart = _connectionString[(slashIndex + 1)..];
        if (int.TryParse(databasePart, out var database))
            _parsedDatabase = database;
    }

    /// <summary>
    ///     Binds from the "ConnectionStrings" section.
    ///     appsettings.json format: "ConnectionStrings": { "RedisConnection": "localhost:6379/1" }
    /// </summary>
    public string RedisConnection
    {
        get => ConnectionString;
        set => ConnectionString = value;
    }
}
