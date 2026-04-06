using Hangfire;

namespace App.Configurations.Extensions;

public static class HangfireQueuesExtension
{
    internal static IServiceCollection AddHangfireQueues(
        this IServiceCollection services)
    {
        services.AddHangfireServer(options =>
        {
            options.Queues = ["default", "heavy"];
            options.WorkerCount = Environment.ProcessorCount * 2;
        });

        return services;
    }
}