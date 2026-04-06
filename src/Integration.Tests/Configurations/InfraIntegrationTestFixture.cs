using Integration.Tests.Helpers;
using MassTransit;
using MessageQueue.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Persistence.Options;
using StackExchange.Redis;
using Testcontainers.LocalStack;
using Testcontainers.Redis;

namespace Integration.Tests.Configurations;

public sealed class InfraIntegrationTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly LocalStackContainer
        _localStackContainer = new LocalStackBuilder("localstack/localstack:4").Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine").Build();

    private HostReceiveEndpointHandle? _testConsumerEndpointHandle;

    public IConnectionMultiplexer Connection { get; private set; } = null!;

    public string RedisConnectionString { get; private set; } = string.Empty;

    public string LocalStackServiceUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _redisContainer.StartAsync(),
            _localStackContainer.StartAsync());

        RedisConnectionString = _redisContainer.GetConnectionString();
        LocalStackServiceUrl = _localStackContainer.GetConnectionString();

        Connection = await ConnectionMultiplexer.ConnectAsync(RedisConnectionString);

        // Force the WebApplicationFactory host to start so the bus is running.
        // CreateClient() triggers host creation if not already started.
        using var _ = CreateClient();

        // Dynamically connect the TestProcessAssetEventConsumer to the running MassTransit bus.
        // This creates a separate SQS queue subscribed to the ProcessAssetEvent SNS topic,
        // without replacing the existing bus configuration (avoids double AddMassTransit).
        var bus = Services.GetRequiredService<IBus>();
        var messageStore = Services.GetRequiredService<ConsumedMessageStore>();

        _testConsumerEndpointHandle = bus.ConnectReceiveEndpoint(
            "integration-test-process-asset-event",
            endpoint => { endpoint.Consumer(() => new TestProcessAssetEventConsumer(messageStore)); });

        await _testConsumerEndpointHandle.Ready;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_testConsumerEndpointHandle is not null)
            await _testConsumerEndpointHandle.StopAsync(CancellationToken.None);

        // Stop the WebApplicationFactory host (Hangfire server + MassTransit bus) BEFORE
        // tearing down containers, so no component tries to reach an already-stopped container.
        Dispose();

        Connection.Dispose();

        await Task.WhenAll(
            _redisContainer.StopAsync(),
            _localStackContainer.StopAsync());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // Override ConnectionStrings:RedisConnection so HangfireExtension reads the container address.
        // Format: "host:port/database" — HangfireExtension parses HostConnectionString and Database from this.
        builder.UseSetting("ConnectionStrings:RedisConnection", $"{RedisConnectionString}/1");

        builder.ConfigureServices(services =>
        {
            // Remove all existing RedisOption configuration registrations and re-register
            // pointing to the test container. Uses RemoveAll to handle multiple registrations
            // (e.g., IConfigureOptions + IOptionsChangeTokenSource from .Bind()).
            services.RemoveAll<IConfigureOptions<RedisOption>>();
            services.Configure<RedisOption>(opt => opt.RedisConnection = $"{RedisConnectionString}/1");

            // Replace IConnectionMultiplexer with the test container connection
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton(Connection);

            // Remove all existing AwsOption configuration registrations and re-register
            // pointing to LocalStack. RemoveAll handles multiple IConfigureOptions registrations.
            services.RemoveAll<IConfigureOptions<AwsOption>>();
            services.Configure<AwsOption>(opt =>
            {
                opt.ServiceUrl = LocalStackServiceUrl;
                opt.Sqs = new SqsOption
                {
                    Region = "us-east-1",
                    AccessKey = "test",
                    SecretKey = "test",
                    Scope = "integration-test",
                    RetryCount = 0,
                    IntervalMilliSeconds = 0
                };
            });

            // Register ConsumedMessageStore for event consumption verification.
            // The TestProcessAssetEventConsumer is connected dynamically via
            // IBus.ConnectReceiveEndpoint in InitializeAsync (after the host starts),
            // so there is no second AddMassTransit call that would replace the bus config.
            services.AddSingleton<ConsumedMessageStore>();
        });
    }
}
