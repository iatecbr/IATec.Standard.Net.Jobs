using FluentValidation;

namespace Application.Features.Assets.Rules;

/// <summary>
///     Shared validation rules for asset-related properties.
///     These rules are reused across validators (e.g. <see cref="Commands.ProcessAssetCommandValidator" />).
///     Using extension methods on <see cref="IRuleBuilderOptions{T,TProperty}" />
///     ensures a single source of truth for validation logic.
/// </summary>
public static class AssetValidationRules
{
    public static IRuleBuilderOptions<T, string> IsValidAssetName<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("The asset name is required.")
            .MaximumLength(200)
            .WithMessage("The asset name must not exceed 200 characters.");
    }

    public static IRuleBuilderOptions<T, string> IsValidAssetCode<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("The asset code is required.")
            .Matches(@"^[A-Z]{2,5}-\d{4,10}$")
            .WithMessage("The code must follow the format 'XX-0000' (2-5 uppercase letters, hyphen, 4-10 digits).");
    }

    public static IRuleBuilderOptions<T, decimal> IsValidAssetValue<T>(
        this IRuleBuilder<T, decimal> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0)
            .WithMessage("The asset value must be greater than zero.");
    }

    public static IRuleBuilderOptions<T, DateTime> IsValidAcquisitionDate<T>(
        this IRuleBuilder<T, DateTime> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("The acquisition date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("The acquisition date cannot be a future date.");
    }
}
