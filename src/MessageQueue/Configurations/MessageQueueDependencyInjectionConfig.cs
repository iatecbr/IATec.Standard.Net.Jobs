using MessageQueue.Configurations.Extensions;
using MessageQueue.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MessageQueue.Configurations;

public static class MessageQueueDependencyInjectionConfig
{
    public static IServiceCollection ConfigureMessageQueue(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AwsOption>(configuration.GetSection(AwsOption.Key).Bind);

        services.AddMassTransitWithSqs();

        return services;
    }
}