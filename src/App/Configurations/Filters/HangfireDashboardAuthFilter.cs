using System.Net.Http.Headers;
using System.Text;
using App.Configurations.Options;
using Hangfire.Dashboard;

namespace App.Configurations.Filters;

public class HangfireDashboardAuthFilter(DashboardOption dashboardOption) : IDashboardAuthorizationFilter
{
    private readonly string _password = dashboardOption.Password;
    private readonly string _username = dashboardOption.Username;

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var authHeader = httpContext.Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader))
        {
            SetUnauthorizedResponse(httpContext);
            return false;
        }

        if (!AuthenticationHeaderValue.TryParse(authHeader, out var parsed)
            || parsed.Scheme != "Basic"
            || string.IsNullOrEmpty(parsed.Parameter))
        {
            SetUnauthorizedResponse(httpContext);
            return false;
        }

        var decodedBytes = Convert.FromBase64String(parsed.Parameter);
        var credentials = Encoding.UTF8.GetString(decodedBytes).Split(':', 2);

        if (credentials.Length != 2
            || credentials[0] != _username
            || credentials[1] != _password)
        {
            SetUnauthorizedResponse(httpContext);
            return false;
        }

        return true;
    }

    private static void SetUnauthorizedResponse(HttpContext httpContext)
    {
        httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }
}