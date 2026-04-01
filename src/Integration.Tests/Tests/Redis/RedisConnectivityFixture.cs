using Bogus;
using Integration.Tests.Configurations;
using StackExchange.Redis;

namespace Integration.Tests.Tests.Redis;

[CollectionDefinition(nameof(RedisConnectivityFixtureCollection))]
public class RedisConnectivityFixtureCollection
    : ICollectionFixture<InfraIntegrationTestFixture>,
      ICollectionFixture<RedisConnectivityFixture>;

public sealed class RedisConnectivityFixture
{
    public readonly Faker Faker = new();

    public IConnectionMultiplexer Connection = null!;

    public IDatabase GetDatabase() => Connection.GetDatabase();
}
