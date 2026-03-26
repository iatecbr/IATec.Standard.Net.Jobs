using Application.Configurations.Extensions;
using Application.Dispatchers.Logging;
using Application.Helpers;
using Application.Services;
using Domain.Contracts.Helpers;
using Domain.Contracts.Services;
using IATec.Shared.Domain.Contracts.Dispatcher;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Configurations;

public static class ApplicationDependencyInjectionConfig
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection services)
    {
        services.AddMediator().AddValidators();

        services.AddScoped<ILogDispatcher, LogDispatcher>();

        // JobHelper is Singleton — it holds a ConcurrentDictionary of progress bars
        // that needs to persist across job executions
        services.AddSingleton<IJobHelper, JobHelper>();

        // BatchJobService is Scoped — creates Hangfire Pro batches with automatic monitoring
        services.AddScoped<IBatchJobService, BatchJobService>();

        return services;
    }
}