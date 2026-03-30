using Application.Configurations.Options;
using Application.Contracts;
using Microsoft.Extensions.Options;

namespace Application.RecurringJobs;

public class VerifyDocumentStatus(
    IRecurringJobFactory recurringJobFactory,
    IOptions<UrlsServiceClientOption> urlOptions,
    IOptions<CronExpressionOption> cronOptions)
{
    public void Execute()
    {
        recurringJobFactory.CreateJob<IJobDispatcher>("VerifyDocumentStatusJob",
            dispatcher => dispatcher.DispatchAsync(urlOptions.Value.Accounts.Url), cronOptions.Value.EveryMinute);
    }
}