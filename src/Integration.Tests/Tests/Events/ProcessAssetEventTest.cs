using Application.Features.Assets.Events;
using Integration.Tests.Configurations;
using Integration.Tests.Helpers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Events;

[Collection(nameof(JobIntegrationFixtureCollection))]
public class ProcessAssetEventTest(InfraIntegrationTestFixture fixture)
{
    private readonly IServiceProvider _services = fixture.Services;

    [Theory(DisplayName = "Publish ProcessAssetEvent via MassTransit and verify consumer receives it")]
    [Trait("Category", "Integration Test - Events")]
    [MemberData(nameof(JobIntegrationFixture.AssetEvents), MemberType = typeof(JobIntegrationFixture))]
    public async Task ProcessAssetEvent_PublishViaMassTransit_ConsumerReceivesMessage(
        Guid assetId, string code, string name, decimal value)
    {
        // Arrange
        using var scope = _services.CreateScope();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var messageStore = scope.ServiceProvider.GetRequiredService<ConsumedMessageStore>();

        var integrationEvent = new ProcessAssetEvent
        {
            AssetId = assetId,
            Code = code,
            Name = name,
            Value = value
        };

        // Act
        await publishEndpoint.Publish(integrationEvent);

        // Assert — wait for consumer to process the message
        var consumed = await messageStore.WaitForMessageAsync(assetId, TimeSpan.FromSeconds(30));
        Assert.NotNull(consumed);
        Assert.Equal(assetId, consumed.AssetId);
        Assert.Equal(code, consumed.Code);
        Assert.Equal(name, consumed.Name);
        Assert.Equal(value, consumed.Value);
    }
}
