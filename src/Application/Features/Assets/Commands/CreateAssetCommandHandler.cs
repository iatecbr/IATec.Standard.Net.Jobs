using Application.Features.Assets.Events;
using Domain.Contracts.Bus;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Assets.Commands;

public class CreateAssetCommandHandler(
    IMessagePublisher messagePublisher,
    ILogger<CreateAssetCommandHandler> logger) : IRequestHandler<CreateAssetCommand, Result>
{
    public async Task<Result> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        // If execution reaches here, FluentValidation has already passed
        // since ValidatorPipelineBehavior intercepts before the handler

        logger.LogInformation("Creating asset with code {Code}", request.Code);

        // TODO: Implement persistence logic

        var assetCreatedEvent = new AssetCreatedEvent
        {
            Name = request.Name,
            Description = request.Description,
            Code = request.Code,
            Value = request.Value,
            AcquisitionDate = request.AcquisitionDate,
            Category = request.Category
        };

        await messagePublisher.PublishAsync(assetCreatedEvent, cancellationToken);

        logger.LogInformation("AssetCreatedEvent published for asset {Code}", request.Code);

        return Result.Ok();
    }
}