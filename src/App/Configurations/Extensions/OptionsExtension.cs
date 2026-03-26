using Domain.Options;
using IATec.Shared.Domain.Options;

namespace App.Configurations.Extensions;

public static class OptionsExtension
{
    internal static IServiceCollection AddOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LogServiceOption>(configuration.GetSection(LogServiceOption.Key));
        services.Configure<ContainerOption>(configuration.GetSection(ContainerOption.Key));
        services.Configure<AwsOption>(configuration.GetSection(AwsOption.Key));
        services.Configure<RedisOption>(configuration.GetSection(RedisOption.Key));

        return services;
    }
}