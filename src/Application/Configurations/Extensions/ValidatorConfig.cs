using Application.Configurations.Factories;
using Application.Features.Assets.Validators;
using FluentValidation;
using IATec.Shared.Domain.Contracts.Validator;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Configurations.Extensions;

public static class ValidatorConfig
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateAssetValidator>();
        services.AddScoped<IValidatorGeneric, ValidatorFactory>();

        return services;
    }
}