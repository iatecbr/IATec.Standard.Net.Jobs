using Integration.Tests.Configurations;

namespace Integration.Tests.Tests;

[CollectionDefinition(nameof(JobIntegrationFixtureCollection))]
public class JobIntegrationFixtureCollection : ICollectionFixture<InfraIntegrationTestFixture>,
    ICollectionFixture<JobIntegrationFixture>;

public sealed class JobIntegrationFixture
{
    public static TheoryData<Guid, string, string, decimal> SingleAssetCommands => new()
    {
        { Guid.NewGuid(), "ASSET-0001", "Integration Test Asset Alpha", 150.50m },
        { Guid.NewGuid(), "ASSET-0002", "Integration Test Asset Beta", 999.99m }
    };

    public static TheoryData<int> BatchSizes => new() { 2, 5 };

    public static TheoryData<Guid, string, string, decimal> AssetEvents => new()
    {
        { Guid.NewGuid(), "EVT-0001", "Event Integration Asset Alpha", 100.50m },
        { Guid.NewGuid(), "EVT-0002", "Event Integration Asset Beta", 250.75m },
        { Guid.NewGuid(), "EVT-0003", "Event Integration Asset Gamma", 0.01m }
    };
}
