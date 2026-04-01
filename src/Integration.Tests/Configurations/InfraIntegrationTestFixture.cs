using Hangfire;
using Hangfire.Console;
using Hangfire.Pro.Redis;
using MessageQueue.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Persistence.Options;
using StackExchange.Redis;
using Testcontainers.LocalStack;
using Testcontainers.Redis;

namespace Integration.Tests.Configurations;

public sealed class InfraIntegrationTestFixture : WebApplicationFactory<Program>, IAsyncLifetime, IDisposable
{
    private static readonly Lock HangfireInitLock = new();
    private static bool _hangfireInitialized;

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine").Build();
    private readonly LocalStackContainer _localStackContainer = new LocalStackBuilder("localstack/localstack:4").Build();

    public IConnectionMultiplexer Connection { get; private set; } = null!;

    public string RedisConnectionString { get; private set; } = string.Empty;

    public string LocalStackServiceUrl { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            var redisOptionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IConfigureOptions<RedisOption>));

            if (redisOptionDescriptor is not null)
                services.Remove(redisOptionDescriptor);

            services.Configure<RedisOption>(opt =>
            {
                opt.RedisConnection = $"{RedisConnectionString}/1";
            });

            var multiplexerDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IConnectionMultiplexer));

            if (multiplexerDescriptor is not null)
                services.Remove(multiplexerDescriptor);

            services.AddSingleton(Connection);

            var awsOptionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IConfigureOptions<AwsOption>));

            if (awsOptionDescriptor is not null)
                services.Remove(awsOptionDescriptor);

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
        });
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _redisContainer.StartAsync(),
            _localStackContainer.StartAsync());

        RedisConnectionString = _redisContainer.GetConnectionString();
        LocalStackServiceUrl = _localStackContainer.GetConnectionString();

        Connection = await ConnectionMultiplexer.ConnectAsync(RedisConnectionString);

        lock (HangfireInitLock)
        {
            if (!_hangfireInitialized)
            {
                GlobalConfiguration.Configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseRedisStorage(RedisConnectionString, new RedisStorageOptions
                    {
                        Prefix = "hangfire:",
                        Database = 1
                    })
                    .UseBatches()
                    .UseConsole();

                _hangfireInitialized = true;
            }
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Connection.Dispose();

        await Task.WhenAll(
            _redisContainer.StopAsync(),
            _localStackContainer.StopAsync());
    }

    public new void Dispose()
    {
        GC.SuppressFinalize(this);
        base.Dispose();
    }
}
