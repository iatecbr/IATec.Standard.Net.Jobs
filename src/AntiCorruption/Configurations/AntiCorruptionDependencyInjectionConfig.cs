using AntiCorruption.Configurations.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AntiCorruption.Configurations;

public static class AntiCorruptionDependencyInjectionConfig
{
    public static IServiceCollection ConfigureAntiCorruption(this IServiceCollection services)
    {
        return services
            .AddLoggingService();
    }
}