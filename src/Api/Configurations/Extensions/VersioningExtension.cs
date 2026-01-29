using Asp.Versioning;

namespace Api.Configurations.Extensions;

public static class VersioningExtension
{
    internal static IServiceCollection AddVersioning(
        this IServiceCollection services)
    {
        services.AddApiVersioning(option =>
        {
            option.AssumeDefaultVersionWhenUnspecified = true;
            option.DefaultApiVersion = new ApiVersion(1, 0);
            option.ReportApiVersions = true;
            option.ApiVersionReader = ApiVersionReader.Combine(
                new QueryStringApiVersionReader("api-version"));
        }).AddApiExplorer(options =>
        {
            // ReSharper disable once StringLiteralTypo
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
        return services;
    }
}