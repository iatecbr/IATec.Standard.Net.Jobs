using Domain.Options;
using Hangfire;
using Hangfire.Console;
using Hangfire.Pro.Redis;

namespace App.Configurations.Extensions;

public static class HangfireExtension
{
    internal static IServiceCollection AddHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisOption = new RedisOption();
        configuration.GetSection(RedisOption.Key).Bind(redisOption);

        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseRedisStorage(redisOption.ConnectionString, new RedisStorageOptions
                {
                    Prefix = "hangfire:",
                    Database = redisOption.Database
                })
                .UseBatches()
                .UseThrottling()
                .UseConsole();
        });

        return services;
    }

    internal static WebApplication UseAppHangfire(this WebApplication app)
    {
        app.UseHangfireDashboard("/dashboard", new DashboardOptions
        {
            DashboardTitle = "IATec Jobs Dashboard"
            // In production, add authorization filter:
            // Authorization = [new HangfireAuthorizationFilter()]
        });

        return app;
    }
}