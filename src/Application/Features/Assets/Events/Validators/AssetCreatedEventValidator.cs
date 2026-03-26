using FluentValidation;

namespace Application.Features.Assets.Events.Validators;

public class AssetCreatedEventValidator : AbstractValidator<AssetCreatedEvent>
{
    public AssetCreatedEventValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("The asset name is required.")
            .MaximumLength(200)
            .WithMessage("The asset name must not exceed 200 characters.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("The asset code is required.")
            .Matches(@"^[A-Z]{2,5}-\d{4,10}$")
            .WithMessage("The code must follow the format 'XX-0000' (2-5 uppercase letters, hyphen, 4-10 digits).");

        RuleFor(x => x.Value)
            .GreaterThan(0)
            .WithMessage("The asset value must be greater than zero.");

        RuleFor(x => x.AcquisitionDate)
            .NotEmpty()
            .WithMessage("The acquisition date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("The acquisition date cannot be a future date.");

        RuleFor(x => x.CreatedAt)
            .NotEmpty()
            .WithMessage("The creation timestamp is required.");
    }
}