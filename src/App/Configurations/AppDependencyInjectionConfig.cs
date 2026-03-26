using App.Configurations.Extensions;

namespace App.Configurations;

public static class AppDependencyInjectionConfig
{
    public static IServiceCollection ConfigureApp(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Map Controllers (must be before AddVersioning so route constraints are registered)
        services.AddControllers();

        // Add Local Extensions
        services.AddScalar()
            .AddCorsPolicy()
            .AddHealthCheck(environment)
            .AddVersioning()
            .AddOptions(configuration)
            .AddHangfire(configuration)
            .AddHangfireQueues();

        return services;
    }

    public static WebApplication UseApp(this WebApplication app)
    {
        // Use Local Extensions
        app.UseAppScalar()
            .UseAppCorsPolicy()
            .UseAppHealthChecks()
            .UseAppHangfire()
            .ApplyMigrations();

        app.UseRouting();
        app.MapControllers();

        return app;
    }
}