using Bogus;
using Integration.Tests.Configurations;

namespace Integration.Tests.Tests.Sqs;

[CollectionDefinition(nameof(SqsPublishConsumeFixtureCollection))]
public class SqsPublishConsumeFixtureCollection
    : ICollectionFixture<InfraIntegrationTestFixture>,
      ICollectionFixture<SqsPublishConsumeFixture>;

public sealed class SqsPublishConsumeFixture
{
    public readonly Faker Faker = new();

    public string LocalStackServiceUrl = string.Empty;
}
