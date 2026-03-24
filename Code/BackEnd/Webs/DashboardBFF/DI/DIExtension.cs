using Microsoft.Extensions.DependencyInjection;
using Services.Auth;
using Services.Redirect;
using VNM.Infrastructure.Auth;

namespace VNM.Infrastructure.Extensions;

/// <summary>
/// Registers application-level services used by the BFF.
/// </summary>
public static class ApplicationServicesExtensions
{
    /// <summary>
    /// Registers BFF application services.
    /// </summary>
    public static IServiceCollection AddBffApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserPermissionResolver, DbUserPermissionResolver>();

        return services;
    }
}