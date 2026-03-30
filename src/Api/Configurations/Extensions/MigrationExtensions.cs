namespace Api.Configurations.Extensions;

public static class MigrationExtensions
{
    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        if (app.Environment.EnvironmentName is "Local") return app;

        return app;
    }
}