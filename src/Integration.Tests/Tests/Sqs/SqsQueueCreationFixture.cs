using Bogus;
using Integration.Tests.Configurations;

namespace Integration.Tests.Tests.Sqs;

[CollectionDefinition(nameof(SqsQueueCreationFixtureCollection))]
public class SqsQueueCreationFixtureCollection
    : ICollectionFixture<InfraIntegrationTestFixture>,
      ICollectionFixture<SqsQueueCreationFixture>;

public sealed class SqsQueueCreationFixture
{
    public readonly Faker Faker = new();

    public string LocalStackServiceUrl = string.Empty;
}
