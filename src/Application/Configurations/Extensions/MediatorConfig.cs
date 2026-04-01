using HangFire.Jobs.Commands;
using IATec.Shared.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Configurations.Extensions;

public static class MediatorConfig
{
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(MediatorConfig).Assembly);
            config.RegisterServicesFromAssembly(typeof(MonitorBatchCommand).Assembly);

            // Pipeline order:
            // 1. Validate the command (returns Result.Fail on validation errors — handler never runs)
            // 2. Catch unhandled exceptions in handlers (returns Result with InternalServerError)
            config.AddOpenBehavior(typeof(ValidatorPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(ExceptionPipelineBehavior<,>));
        });

        return services;
    }
}