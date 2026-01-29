using IATec.Shared.Domain.Contracts.Dispatcher;
using IATec.Shared.Domain.Contracts.Services.Logging;
using IATec.Shared.Domain.Models.LoggingAggregate.Dtos;
using IATec.Shared.Domain.Options;
using Microsoft.Extensions.Options;

namespace Application.Dispatchers.Logging;

public class LogDispatcher(ILogService logService, IOptions<ContainerOption> options) : ILogDispatcher
{
    public async Task DispatchAsync(
        string source,
        string owner,
        string action,
        object? content = null,
        CancellationToken cancellationToken = default
    )
    {
        var logDto = new LogDto
        {
            ContainerKey = options.Value.ContainerId,
            Source = source,
            Owner = owner,
            Action = action,
            Content = content?.ToString()!,
            Date = DateTime.UtcNow
        };

        await logService.SendAsync(logDto, cancellationToken);
    }
}