using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Validation;

public static class FluentValidationExtensions
{
    public static IServiceCollection AddAppValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AddressDtoValidator>();
        return services;
    }
}
