using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Api.Configurations.Extensions;

public static class SwaggerExtension
{
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Standard", Version = "v1" });
            options.TryIncludeXmlComments();
            options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });

            options.AddSecurityRequirement(_ =>
            {
                OpenApiSecuritySchemeReference schemeRef = new("Bearer");
                OpenApiSecurityRequirement requirement = new()
                {
                    [schemeRef] = []
                };
                return requirement;
            });
        });

        return services;
    }

    public static WebApplication UseApiSwagger(this WebApplication app)
    {
        if (app.Environment.EnvironmentName is not ("Local" or "Development")) return app;

        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => { c.DocExpansion(DocExpansion.None); });

        return app;
    }

    private static string GetXmlPath()
    {
        return Assembly.GetEntryAssembly()?.Location.Replace("dll", "xml") ??
               "/app/bin/Debug/net10.0/Api.xml";
    }

    private static void TryIncludeXmlComments(this SwaggerGenOptions c)
    {
        try
        {
            var xmlPath = GetXmlPath();
            c.IncludeXmlComments(xmlPath);
        }
        catch
        {
            Console.WriteLine("Xml not found, swagger docstring will be no get");
        }
    }
}