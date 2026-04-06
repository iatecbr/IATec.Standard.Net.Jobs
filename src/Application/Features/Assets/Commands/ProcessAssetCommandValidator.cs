using Application.Features.Assets.Rules;
using FluentValidation;

namespace Application.Features.Assets.Commands;

/// <summary>
///     Validator for <see cref="ProcessAssetCommand" />.
///     Automatically invoked by <c>ValidatorPipelineBehavior</c> in the MediatR pipeline.
///     If validation fails, the pipeline short-circuits with <c>Result.Fail()</c> —
///     the handler never runs and no exception is thrown.
/// </summary>
public class ProcessAssetCommandValidator : AbstractValidator<ProcessAssetCommand>
{
    public ProcessAssetCommandValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty()
            .WithMessage("The asset ID is required.");

        RuleFor(x => x.Name).IsValidAssetName();

        RuleFor(x => x.Code).IsValidAssetCode();

        RuleFor(x => x.Value).IsValidAssetValue();
    }
}