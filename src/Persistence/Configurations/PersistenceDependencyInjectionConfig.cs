using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Configurations.Extensions;

namespace Persistence.Configurations;

public static class PersistenceDependencyInjectionConfig
{
    public static void ConfigurePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddData(configuration);
    }
}