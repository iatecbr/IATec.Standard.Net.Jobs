using Application.Configurations.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Configurations.Extensions;

public static class OptionsExtension
{
    internal static IServiceCollection AddOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<UrlsServiceClientOption>(configuration.GetSection(UrlsServiceClientOption.Key));
        services.Configure<CronExpressionOption>(configuration.GetSection(CronExpressionOption.Key));

        return services;
    }
}