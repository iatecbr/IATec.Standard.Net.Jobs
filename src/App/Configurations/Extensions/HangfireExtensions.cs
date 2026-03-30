using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace App.Configurations.Extensions;

/// <summary>
/// Configuration extensions for Hangfire, a library for background job processing in .NET applications.
/// This class provides methods to set up Hangfire services and configure it to use Redis as the storage
/// mechanism for background jobs.
/// </summary>
public static class HangfireExtensions
{
    /// <summary>
    /// Add Hangfire services to the application, using Redis as the storage mechanism.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseRedisStorage(configuration.GetConnectionString("RedisConnection")));

        services.AddHangfireServer();
    }
}