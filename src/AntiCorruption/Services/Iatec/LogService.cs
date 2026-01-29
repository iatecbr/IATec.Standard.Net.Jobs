using System.Net.Http.Json;
using IATec.Shared.Domain.Contracts.Services.Logging;
using IATec.Shared.Domain.Models.LoggingAggregate.Dtos;
using Microsoft.Extensions.Logging;

namespace AntiCorruption.Services.Iatec;

public class LogService(HttpClient client, ILogger<LogService> logger) : ILogService
{
    public async Task SendAsync(LogDto log, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Sending log to Iatec Log Service");

            await client.PostAsJsonAsync("v1/log", log, cancellationToken);

            logger.LogInformation("Log sent to Iatec successfully");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error sending log to Iatec");
        }
    }
}