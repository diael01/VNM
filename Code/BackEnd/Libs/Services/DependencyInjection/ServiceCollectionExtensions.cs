using Microsoft.Extensions.DependencyInjection;
using Services.Authorization;
using Services.Inverter;
using Services.Identity;
using Services.Redirect;

namespace Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
      
    {
        services.AddScoped<IInverterReadingService, InverterReadingService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IAspNetIdentityService, AspNetIdentityService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IInverterInfoService, InverterInfoService>();

        // Register BFF redirect services
        services.AddScoped<IDashboardAddressRedirectService, DashboardAddressRedirectService>();
        services.AddScoped<IDashboardInverterRedirectService, DashboardInverterRedirectService>();
        return services;
    }
}
