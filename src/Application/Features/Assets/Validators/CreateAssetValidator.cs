using Application.Features.Assets.Commands;
using FluentValidation;

namespace Application.Features.Assets.Validators;

public class CreateAssetValidator : AbstractValidator<CreateAssetCommand>
{
    public CreateAssetValidator()
    {
    }
}