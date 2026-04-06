using System.Reflection;
using App.Configurations.Filters;
using App.Configurations.Options;
using Hangfire;
using Hangfire.Common;
using Hangfire.Console;
using Hangfire.Dashboard;
using HangFire.Jobs.Filters;
using Hangfire.Pro.Redis;
using MediatR;
using Persistence.Options;

namespace App.Configurations.Extensions;

public static class HangfireExtension
{
    private static readonly Lock ConsoleInitLock = new();
    private static bool _consoleInitialized;

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
                .UseRedisStorage(redisOption.HostConnectionString, new RedisStorageOptions
                {
                    Prefix = "hangfire:",
                    Database = redisOption.Database
                })
                .UseBatches()
                .UseThrottling();

            // UseConsole() throws InvalidOperationException if called more than once per process.
            // Multiple WebApplicationFactory instances in integration tests trigger this callback
            // multiple times, so guard with a static flag.
            lock (ConsoleInitLock)
            {
                if (!_consoleInitialized)
                {
                    config.UseConsole();
                    _consoleInitialized = true;
                }
            }

            // Remove Hangfire's built-in AutomaticRetryAttribute (Attempts=10, Order=20)
            // to prevent it from overriding retry policies defined on command records.
            // BaseCommand<T> retry is fully managed by CommandAttributeJobFilter,
            // which propagates [AutomaticRetry] from command types to job filters.
            RemoveDefaultAutoRetryFilter();

            // Propagates [AutomaticRetry], [Queue], and [CommandDisplayName] from command records
            // to jobs enqueued via ISender.Send(command).
            // Order = -1 ensures this filter runs before any other global filter.
            GlobalJobFilters.Filters.Add(new CommandAttributeJobFilter { Order = -1 });
        });

        return services;
    }

    internal static WebApplication UseAppHangfire(this WebApplication app)
    {
        var dashboardOption = new DashboardOption();
        app.Configuration.GetSection(DashboardOption.Key).Bind(dashboardOption);

        app.UseHangfireDashboard("/dashboard", new DashboardOptions
        {
            DashboardTitle = "IATec Jobs Dashboard",

            // Display name resolution for command-based jobs:
            // When a job is enqueued via ISender.Send(command), the method is ISender.Send()
            // which has no [JobDisplayName]. The Hangfire Dashboard fallback chain is:
            //   1. [JobDisplayName] on method → not present for ISender.Send()
            //   2. [DisplayName] (System.ComponentModel) on method → not present
            //   3. DisplayNameFunc (this) → reads [CommandDisplayName] from the command type
            //   4. Job.ToString() → "ISender.Send (queue)"
            DisplayNameFunc = ResolveCommandDisplayName,

            // Basic Authentication: browser prompts for username/password
            // Credentials are configured in appsettings.json under "Dashboard" section
            Authorization = [new HangfireDashboardAuthFilter(dashboardOption)]
        });

        return app;
    }

    /// <summary>
    ///     Resolves the display name for command-based jobs enqueued via <c>ISender.Send(command)</c>.
    ///     Searches the <see cref="Job.Args" /> for an <see cref="IBaseRequest" /> argument,
    ///     reads <see cref="CommandDisplayNameAttribute" /> from its type, and formats
    ///     using the command's <c>ToString()</c>.
    ///     Returns <c>null</c> if no command argument or no <see cref="CommandDisplayNameAttribute" />
    ///     is found, letting Hangfire fall back to <c>Job.ToString()</c>.
    /// </summary>
    private static string? ResolveCommandDisplayName(DashboardContext context, Job job)
    {
        if (job.Args is null)
            return null;

        foreach (var arg in job.Args)
        {
            if (arg is not IBaseRequest)
                continue;

            var commandType = arg.GetType();
            var displayAttr = commandType.GetCustomAttribute<CommandDisplayNameAttribute>();

            if (displayAttr is null)
                return null;

            var formatted = string.Format(displayAttr.DisplayName, arg.ToString() ?? string.Empty);
            return formatted;
        }

        return null;
    }

    /// <summary>
    ///     Removes the default <see cref="AutomaticRetryAttribute" /> that Hangfire adds
    ///     to <see cref="GlobalJobFilters" /> automatically (Attempts=10, Order=20).
    ///     This prevents it from overriding retry policies propagated from command attributes
    ///     by <see cref="CommandAttributeJobFilter" />.
    /// </summary>
    private static void RemoveDefaultAutoRetryFilter()
    {
        var filtersToRemove = GlobalJobFilters.Filters
            .Where(f => f.Instance is AutomaticRetryAttribute)
            .ToList();

        foreach (var filter in filtersToRemove)
            GlobalJobFilters.Filters.Remove(filter.Instance);
    }
}