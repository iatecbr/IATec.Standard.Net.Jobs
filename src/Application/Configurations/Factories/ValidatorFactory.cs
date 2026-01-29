using FluentValidation;
using IATec.Shared.Domain.Contracts.Validator;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Configurations.Factories;

public class ValidatorFactory(IServiceProvider serviceProvider) : IValidatorGeneric
{
    public IValidator<T>? GetValidator<T>()
    {
        var validator = serviceProvider.GetService<IValidator<T>>();
        return validator;
    }
}