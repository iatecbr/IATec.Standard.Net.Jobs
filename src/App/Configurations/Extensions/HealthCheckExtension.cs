using System.Reflection;
using Persistence.Options;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace App.Configurations.Extensions;

/// <summary>
///     Health Check Configuration
/// </summary>
public static class HealthCheckExtension
{
    internal static IServiceCollection AddHealthCheck(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var redisOption = new RedisOption();
        configuration.GetSection(RedisOption.Key).Bind(redisOption);

        services.AddHealthChecks()
            .AddCheck<VersionHealthCheck>("Version", HealthStatus.Healthy, [environment.EnvironmentName])
            .AddRedis(redisOption.HostConnectionString, "Redis (Hangfire)", HealthStatus.Unhealthy, ["infrastructure"])
            .AddHangfire(setup =>
            {
                setup.MaximumJobsFailed = 5;
                setup.MinimumAvailableServers = 1;
            }, "Hangfire", HealthStatus.Degraded, ["infrastructure"]);

        return services;
    }

    /// <summary>
    ///     Use Health Check
    /// </summary>
    /// <param name="app"></param>
    public static WebApplication UseAppHealthChecks(this WebApplication app)
    {
        app.UseHealthChecks("/_healthcheck/status", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}

internal class VersionHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy(Assembly.GetEntryAssembly()?.GetName().Version?.ToString()));
    }
}