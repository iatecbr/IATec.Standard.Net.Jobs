using Bogus;
using Integration.Tests.Configurations;
using StackExchange.Redis;

namespace Integration.Tests.Tests.Redis;

[CollectionDefinition(nameof(RedisBatchProgressFixtureCollection))]
public class RedisBatchProgressFixtureCollection
    : ICollectionFixture<InfraIntegrationTestFixture>,
      ICollectionFixture<RedisBatchProgressFixture>;

public sealed class RedisBatchProgressFixture
{
    public readonly Faker Faker = new();

    public IConnectionMultiplexer Connection = null!;
}
