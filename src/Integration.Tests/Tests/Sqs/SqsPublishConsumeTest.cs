using Amazon.SQS;
using Amazon.SQS.Model;
using Application.Features.Assets.Events;
using Integration.Tests.Configurations;
using Integration.Tests.Helpers;
using MassTransit;
using MessageQueue.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Integration.Tests.Tests.Sqs;

[Collection(nameof(SqsPublishConsumeFixtureCollection))]
public class SqsPublishConsumeTest : IAsyncLifetime
{
    private readonly SqsPublishConsumeFixture _sqsPublishConsumeFixture;
    private ServiceProvider? _provider;

    private ServiceProvider Provider =>
        _provider ?? throw new InvalidOperationException("ServiceProvider not initialized. InitializeAsync must run first.");

    public SqsPublishConsumeTest(
        SqsPublishConsumeFixture sqsPublishConsumeFixture,
        InfraIntegrationTestFixture infraIntegrationTestFixture)
    {
        _sqsPublishConsumeFixture = sqsPublishConsumeFixture;

        _sqsPublishConsumeFixture.LocalStackServiceUrl = infraIntegrationTestFixture.LocalStackServiceUrl;
    }

    public static TheoryData<Guid, string, string, decimal> AssetEvents => new()
    {
        { Guid.NewGuid(), "ASSET-0001", "Integration Test Asset Alpha", 100.50m },
        { Guid.NewGuid(), "ASSET-0002", "Integration Test Asset Beta", 250.75m },
        { Guid.NewGuid(), "ASSET-0003", "Integration Test Asset Gamma", 0.01m }
    };

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.Configure<AwsOption>(opt =>
        {
            opt.ServiceUrl = _sqsPublishConsumeFixture.LocalStackServiceUrl;
            opt.Sqs = new SqsOption
            {
                Region = "us-east-1",
                AccessKey = "test",
                SecretKey = "test",
                Scope = $"integration-test-{Guid.NewGuid():N}",
                RetryCount = 0,
                IntervalMilliSeconds = 0
            };
        });

        services.AddSingleton<ConsumedMessageStore>();

        services.AddMassTransit(busConfig =>
        {
            busConfig.AddConsumer<TestProcessAssetEventConsumer>();

            busConfig.UsingAmazonSqs((context, cfg) =>
            {
                var awsOption = context.GetRequiredService<IOptions<AwsOption>>().Value;

                cfg.Host(awsOption.Sqs!.Region, hostConfig =>
                {
                    hostConfig.AccessKey(awsOption.Sqs.AccessKey);
                    hostConfig.SecretKey(awsOption.Sqs.SecretKey);

                    if (!string.IsNullOrEmpty(awsOption.Sqs.Scope))
                        hostConfig.Scope(awsOption.Sqs.Scope, true);

                    if (!string.IsNullOrEmpty(awsOption.ServiceUrl))
                    {
                        hostConfig.Config(new AmazonSQSConfig
                        {
                            ServiceURL = awsOption.ServiceUrl
                        });

                        hostConfig.Config(new Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceConfig
                        {
                            ServiceURL = awsOption.ServiceUrl
                        });
                    }
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        _provider = services.BuildServiceProvider();

        var busControl = _provider.GetRequiredService<IBusControl>();
        await busControl.StartAsync();

        await Task.Delay(3000);
    }

    public async Task DisposeAsync()
    {
        if (_provider is not null)
        {
            var busControl = _provider.GetRequiredService<IBusControl>();
            await busControl.StopAsync();
            await _provider.DisposeAsync();
        }
    }

    [Theory(DisplayName = "Publish ProcessAssetEvent and consume from SQS")]
    [Trait("Category", "SQS Integration Test - PublishConsume")]
    [MemberData(nameof(AssetEvents))]
    public async Task SqsPublishConsume_PublishEvent_DeliversMessage(
        Guid assetId, string code, string name, decimal value)
    {
        // Arrange
        var publishEndpoint = Provider.GetRequiredService<IPublishEndpoint>();
        var messageStore = Provider.GetRequiredService<ConsumedMessageStore>();

        var integrationEvent = new ProcessAssetEvent
        {
            AssetId = assetId,
            Code = code,
            Name = name,
            Value = value
        };

        // Act
        await publishEndpoint.Publish(integrationEvent);

        // Assert
        var consumed = await messageStore.WaitForMessageAsync(assetId, TimeSpan.FromSeconds(30));
        Assert.NotNull(consumed);

        Assert.Equal(assetId, consumed.AssetId);
        Assert.Equal(code, consumed.Code);
        Assert.Equal(name, consumed.Name);
        Assert.Equal(value, consumed.Value);
    }

    [Theory(DisplayName = "Verify SQS queues auto-created by MassTransit")]
    [Trait("Category", "SQS Integration Test - QueueCreation")]
    [InlineData("ProcessAssetEvent")]
    public async Task SqsPublishConsume_StartBus_AutoCreatesQueues(string expectedQueueSubstring)
    {
        // Arrange
        using var client = new AmazonSQSClient(
            "test",
            "test",
            new AmazonSQSConfig
            {
                ServiceURL = _sqsPublishConsumeFixture.LocalStackServiceUrl,
                AuthenticationRegion = "us-east-1"
            });

        // Act
        var listResponse = await client.ListQueuesAsync(new ListQueuesRequest());

        // Assert
        Assert.NotEmpty(listResponse.QueueUrls);
        Assert.Contains(listResponse.QueueUrls,
            url => url.Contains(expectedQueueSubstring, StringComparison.OrdinalIgnoreCase));
    }
}
