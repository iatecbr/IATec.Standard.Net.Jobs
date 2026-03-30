using Application.Configurations.Options;
using Application.Contracts;
using Microsoft.Extensions.Options;

namespace Application.RecurringJobs;

public class DeleteInactiveContainer(
    IRecurringJobFactory recurringJobFactory,
    IOptions<UrlsServiceClientOption> urlOptions,
    IOptions<CronExpressionOption> cronOptions)
{
    public void Execute()
    {
        recurringJobFactory.CreateJob<IJobDispatcher>("DeleteInactiveContainerJob",
            dispatcher => dispatcher.DispatchAsync(urlOptions.Value.Inventory.Url), cronOptions.Value.EveryMinute);
    }
}