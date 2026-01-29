using Application.Configurations.Extensions;
using Application.Dispatchers.Logging;
using IATec.Shared.Domain.Contracts.Dispatcher;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Configurations;

public static class ApplicationDependencyInjectionConfig
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection services)
    {
        services.AddMediator()
            .AddValidators();

        services.AddScoped<ILogDispatcher, LogDispatcher>();

        return services;
    }
}