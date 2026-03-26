using Application.Features.Assets.Jobs;
using Domain.Models.AssetAggregate.Jobs;
using FluentValidation;
using Hangfire;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Application.Features.Assets.Events.Consumers;

/// <summary>
///     MassTransit consumer that acts as a bridge: receives AssetCreatedEvent from SQS,
///     validates it, then enqueues a Hangfire job for actual processing.
///     This keeps the consumer thin — all heavy work is done by Hangfire.
/// </summary>
public class AssetCreatedEventConsumer(
    ILogger<AssetCreatedEventConsumer> logger,
    IValidator<AssetCreatedEvent> validator,
    IBackgroundJobClient backgroundJobClient) : IConsumer<AssetCreatedEvent>
{
    public async Task Consume(ConsumeContext<AssetCreatedEvent> context)
    {
        var message = context.Message;

        logger.LogInformation("Consuming AssetCreatedEvent for asset {Code}", message.Code);

        var validationResult = await validator.ValidateAsync(message, context.CancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogWarning("Validation failed for AssetCreatedEvent {Code}: {Errors}", message.Code, errors);
            throw new ValidationException(validationResult.Errors);
        }

        // Bridge pattern: MassTransit consumer -> Hangfire job
        var jobData = new ProcessAssetDataJobDto
        {
            AssetId = Guid.NewGuid(), // In real scenario, this would come from persistence
            Code = message.Code,
            Name = message.Name,
            Value = message.Value
        };

        backgroundJobClient.Enqueue<ProcessAssetJob>(job => job.ExecuteAsync(jobData, null, CancellationToken.None));

        logger.LogInformation(
            "Hangfire job enqueued for asset {Code} via AssetCreatedEvent", message.Code);
    }
}