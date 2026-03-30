using Microsoft.Extensions.DependencyInjection;

namespace MessageQueue.Configurations;

public static class MessageQueueDependencyInjectionConfig
{
    public static IServiceCollection ConfigureMessageQueue(this IServiceCollection services)
    {
        return services;
    }
}