using Application.Configurations.Extensions;
using Application.Contracts;
using Application.RecurringJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Configurations;

public static class ApplicationDependencyInjectionConfig
{
    public static IServiceCollection ConfigureApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions(configuration);

        services.AddScoped<IJobDispatcher, JobDispatcher>();

        services.AddScoped<VerifyDocumentStatus>();
        services.AddScoped<DeleteInactiveContainer>();

        return services;
    }
}