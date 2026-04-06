using Amazon.SimpleNotificationService;
using Amazon.SQS;
using MassTransit;
using MessageQueue.Consumers;
using MessageQueue.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MessageQueue.Configurations.Extensions;

public static class MassTransitConfig
{
    public static IServiceCollection AddMassTransitWithSqs(this IServiceCollection services)
    {
        services.AddMassTransit(busConfig =>
        {
            // Register event consumers
            busConfig.AddConsumer<ProcessAssetEventConsumer>();

            // Configure Amazon SQS transport
            busConfig.UsingAmazonSqs((context, cfg) =>
            {
                var awsOption = context.GetRequiredService<IOptions<AwsOption>>().Value;

                if (awsOption.Sqs is null)
                    throw new ArgumentNullException(nameof(awsOption.Sqs),
                        $"{AwsOption.Key}:Sqs configuration is required");

                cfg.Host(awsOption.Sqs.Region, hostConfig =>
                {
                    hostConfig.AccessKey(awsOption.Sqs.AccessKey);
                    hostConfig.SecretKey(awsOption.Sqs.SecretKey);

                    // Scope for queue name isolation (e.g., "dev", "staging")
                    if (!string.IsNullOrEmpty(awsOption.Sqs.Scope)) hostConfig.Scope(awsOption.Sqs.Scope, true);

                    // ServiceUrl override for local development (e.g., LocalStack)
                    if (!string.IsNullOrEmpty(awsOption.ServiceUrl))
                    {
                        hostConfig.Config(new AmazonSQSConfig
                        {
                            ServiceURL = awsOption.ServiceUrl
                        });

                        hostConfig.Config(new AmazonSimpleNotificationServiceConfig
                        {
                            ServiceURL = awsOption.ServiceUrl
                        });
                    }
                });

                // Message retry from configuration
                if (awsOption.Sqs.RetryCount > 0)
                    cfg.UseMessageRetry(r =>
                        r.Interval(awsOption.Sqs.RetryCount, awsOption.Sqs.IntervalMilliSeconds));

                cfg.ConfigureEndpoints(context);
            });

            // Long-polling for SQS receive endpoints
            busConfig.AddConfigureEndpointsCallback((_, cfg) =>
            {
                if (cfg is IAmazonSqsReceiveEndpointConfigurator sqs)
                    sqs.WaitTimeSeconds = 20;
            });
        });

        return services;
    }
}