using Asp.Versioning;

namespace App.Configurations.Extensions;

public static class VersioningExtension
{
    internal static IServiceCollection AddVersioning(
        this IServiceCollection services)
    {
        services.AddApiVersioning(option =>
            {
                option.AssumeDefaultVersionWhenUnspecified = true;
                option.DefaultApiVersion = new ApiVersion(2, 0);
                option.ReportApiVersions = true;
                option.ApiVersionReader = new UrlSegmentApiVersionReader();
            }).AddMvc()
            .AddApiExplorer(options =>
            {
                // ReSharper disable once StringLiteralTypo
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
        return services;
    }
}