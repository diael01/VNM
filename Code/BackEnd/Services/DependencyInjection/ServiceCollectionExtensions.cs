using Microsoft.Extensions.DependencyInjection;
using Services.Authorization;
using Services.Inverter;
using Services.Identity;

namespace Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IInverterReadingService, InverterReadingService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IAspNetIdentityService, AspNetIdentityService>();
        return services;
    }
}
