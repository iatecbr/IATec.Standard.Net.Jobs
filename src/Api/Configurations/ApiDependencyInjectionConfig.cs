using Api.Configurations.Extensions;

namespace Api.Configurations;

public static class ApiDependencyInjectionConfig
{
    public static IServiceCollection ConfigureApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Add Local Extensions
        services.AddSwagger()
            .AddCorsPolicy()
            .AddHealthCheck(environment)
            .AddVersioning()
            .AddOptions(configuration);

        // Map Controllers
        services.AddControllers();

        return services;
    }

    public static WebApplication UseApi(this WebApplication app)
    {
        // Use Local Extensions
        app.UseApiSwagger()
            .UseApiCorsPolicy()
            .UseApiHealthChecks()
            .ApplyMigrations();

        app.UseRouting();
        app.MapControllers();

        return app;
    }
}