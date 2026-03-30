using App.Configurations.Extensions;
using App.Factories;
using Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace App.Configurations;

/// <summary>
/// Configures application services and dependencies for the APP layer.
/// </summary>
public static class ConsoleAppDependencyInjectionConfig
{
    /// <summary>
    /// Configure application services and dependencies.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureConsoleApp(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHangfire(configuration);

        services.AddScoped<IRecurringJobFactory, RecurringJobFactory>();

        return services;
    }
}