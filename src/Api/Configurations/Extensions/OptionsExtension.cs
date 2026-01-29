using IATec.Shared.Domain.Options;

namespace Api.Configurations.Extensions;

public static class OptionsExtension
{
    internal static IServiceCollection AddOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LogServiceOption>(configuration.GetSection(LogServiceOption.Key));
        services.Configure<ContainerOption>(configuration.GetSection(ContainerOption.Key));

        return services;
    }
}