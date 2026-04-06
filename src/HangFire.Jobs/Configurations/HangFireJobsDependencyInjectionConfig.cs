using Domain.Contracts.Helpers;
using Domain.Contracts.Services;
using HangFire.Jobs.Contracts;
using HangFire.Jobs.Helpers;
using HangFire.Jobs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HangFire.Jobs.Configurations;

public static class HangFireJobsDependencyInjectionConfig
{
    public static IServiceCollection ConfigureHangFireJobs(this IServiceCollection services)
    {
        // JobHelper is Singleton — it holds a ConcurrentDictionary of progress bars
        // that needs to persist across job executions
        services.AddSingleton<IJobHelper, JobHelper>();

        // PerformContextAccessor — Singleton backed by AsyncLocal<T>,
        // provides PerformContext to command handlers running inside Hangfire workers
        services.AddSingleton<IPerformContextAccessor, PerformContextAccessor>();

        // BatchJobService is Scoped — creates Hangfire Pro batches with automatic monitoring
        services.AddScoped<IBatchJobService, BatchJobService>();

        return services;
    }
}