using System.Reflection;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Configurations.Extensions;

/// <summary>
/// Health Check Configuration
/// </summary>
public static class HealthCheckExtension
{
    internal static IServiceCollection AddHealthCheck(
        this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        services.AddHealthChecks()
            .AddCheck<VersionHealthCheck>("Version", HealthStatus.Healthy, [environment.EnvironmentName]);

        return services;
    }

    /// <summary>
    /// Use Health Check
    /// </summary>
    /// <param name="app"></param>
    public static WebApplication UseApiHealthChecks(this WebApplication app)
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