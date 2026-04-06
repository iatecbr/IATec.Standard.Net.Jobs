using Scalar.AspNetCore;

namespace App.Configurations.Extensions;

public static class ScalarExtension
{
    internal static IServiceCollection AddScalar(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "IATec Standard Jobs API";
                document.Info.Version = "v1";
                document.Info.Description =
                    "Version 1 — contains deprecated endpoints marked for removal in future versions.";
                return Task.CompletedTask;
            });
        });

        services.AddOpenApi("v2", options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "IATec Standard Jobs API";
                document.Info.Version = "v2";
                document.Info.Description = "Version 2 — current stable API.";
                return Task.CompletedTask;
            });
        });

        return services;
    }

    internal static WebApplication UseAppScalar(this WebApplication app)
    {
        if (app.Environment.EnvironmentName is not ("Local" or "Development")) return app;

        app.MapOpenApi();
        app.MapScalarApiReference("documentation", options =>
        {
            options
                .WithTitle("IATec Standard Jobs API")
                .WithTheme(ScalarTheme.Kepler)
                .EnableDarkMode()
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });

        return app;
    }
}