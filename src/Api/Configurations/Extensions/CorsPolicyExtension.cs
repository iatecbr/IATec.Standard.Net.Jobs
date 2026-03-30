namespace Api.Configurations.Extensions;

/// <summary>
/// All cors configurations
/// </summary>
public static class CorsPolicyExtension
{
    /// <summary>
    /// Add cors policy configuration
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
        {
            builder.AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin();

            builder.WithExposedHeaders([
                "X-Custom-Header",
                "Location",
                "Content-Disposition",
                "Content-Length"
            ]);
        }));

        return services;
    }

    /// <summary>
    /// Use cors policy
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication UseApiCorsPolicy(this WebApplication app)
    {
        app.UseCors("CorsPolicy");

        return app;
    }
}