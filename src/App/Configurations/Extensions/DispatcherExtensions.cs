using Application.RecurringJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace App.Configurations.Extensions;

/// <summary>
/// 
/// </summary>
public static class DispatcherExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    public static void UseDispatchers(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var verifyDocumentStatus = scope.ServiceProvider.GetRequiredService<VerifyDocumentStatus>();
        verifyDocumentStatus.Execute();

        var deleteInactiveContainer = scope.ServiceProvider.GetRequiredService<DeleteInactiveContainer>();
        deleteInactiveContainer.Execute();
    }
}