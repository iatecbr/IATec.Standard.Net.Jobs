using Integration.Tests.Configurations;
using StackExchange.Redis;

namespace Integration.Tests.Tests.Redis;

[Collection(nameof(RedisConnectivityFixtureCollection))]
public class RedisConnectivityTest
{
    private readonly RedisConnectivityFixture _redisConnectivityFixture;

    public RedisConnectivityTest(
        RedisConnectivityFixture redisConnectivityFixture,
        InfraIntegrationTestFixture infraIntegrationTestFixture)
    {
        _redisConnectivityFixture = redisConnectivityFixture;

        _redisConnectivityFixture.Connection = infraIntegrationTestFixture.Connection;
    }

    public static TheoryData<string, string> KeyValuePairs => new()
    {
        { "test:key:1", "value-alpha" },
        { "test:key:2", "value-beta" },
        { "test:key:special", "value with spaces and chars: @#$!" }
    };

    public static TheoryData<string, string, TimeSpan> KeyValueWithExpiry => new()
    {
        { "test:expiry:1", "temporary-value", TimeSpan.FromSeconds(30) },
        { "test:expiry:2", "another-temp", TimeSpan.FromMinutes(1) }
    };

    public static TheoryData<string, Dictionary<string, string>> HashData => new()
    {
        {
            "test:hash:1", new Dictionary<string, string>
            {
                { "field1", "value1" },
                { "field2", "value2" },
                { "field3", "100" }
            }
        },
        {
            "test:hash:2", new Dictionary<string, string>
            {
                { "name", "Test Asset" },
                { "code", "ASSET-001" },
                { "value", "99.50" }
            }
        }
    };

    [Theory(DisplayName = "Verify Redis connection is active")]
    [Trait("Category", "Redis Integration Test - Connectivity")]
    [InlineData("PING")]
    public async Task RedisConnectivity_Ping_ConnectionActiveAndResponding(string command)
    {
        // Arrange
        var db = _redisConnectivityFixture.GetDatabase();

        // Act
        var pong = await db.PingAsync();

        // Assert
        Assert.True(_redisConnectivityFixture.Connection.IsConnected);
        Assert.True(pong > TimeSpan.Zero,
            $"Redis should respond to {command}");
    }

    [Theory(DisplayName = "Set and get string values")]
    [Trait("Category", "Redis Integration Test - Connectivity")]
    [MemberData(nameof(KeyValuePairs))]
    public async Task RedisConnectivity_SetGet_RoundTripStringValues(string key, string value)
    {
        // Arrange
        var db = _redisConnectivityFixture.GetDatabase();

        // Act
        await db.StringSetAsync(key, value);
        var retrieved = await db.StringGetAsync(key);

        // Assert
        Assert.True(retrieved.HasValue);
        Assert.Equal(value, retrieved.ToString());

        // Cleanup
        await db.KeyDeleteAsync(key);
    }

    [Theory(DisplayName = "Set and get string values with expiry")]
    [Trait("Category", "Redis Integration Test - Connectivity")]
    [MemberData(nameof(KeyValueWithExpiry))]
    public async Task RedisConnectivity_SetGetWithExpiry_HasCorrectTtl(string key, string value, TimeSpan expiry)
    {
        // Arrange
        var db = _redisConnectivityFixture.GetDatabase();

        // Act
        await db.StringSetAsync(key, value, expiry);
        var ttl = await db.KeyTimeToLiveAsync(key);

        // Assert
        Assert.NotNull(ttl);
        Assert.InRange(ttl.Value, expiry - TimeSpan.FromSeconds(5), expiry + TimeSpan.FromSeconds(5));

        // Cleanup
        await db.KeyDeleteAsync(key);
    }

    [Theory(DisplayName = "Set and retrieve hash fields")]
    [Trait("Category", "Redis Integration Test - Connectivity")]
    [MemberData(nameof(HashData))]
    public async Task RedisConnectivity_HashSetGet_RoundTripAllFields(string hashKey, Dictionary<string, string> fields)
    {
        // Arrange
        var db = _redisConnectivityFixture.GetDatabase();
        var entries = fields.Select(f => new HashEntry(f.Key, f.Value)).ToArray();

        // Act
        await db.HashSetAsync(hashKey, entries);
        var retrieved = await db.HashGetAllAsync(hashKey);

        // Assert
        Assert.Equal(fields.Count, retrieved.Length);

        foreach (var field in fields)
        {
            var hashValue = await db.HashGetAsync(hashKey, field.Key);
            Assert.Equal(field.Value, hashValue.ToString());
        }

        // Cleanup
        await db.KeyDeleteAsync(hashKey);
    }
}
