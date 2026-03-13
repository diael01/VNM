using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VNM.Infrastructure.Extensions;

/// <summary>
/// Provides service collection extensions for JWT bearer authentication.
/// </summary>
public static class JwtAuthenticationExtensions
{
        /// <summary>
    /// Registers JWT bearer authentication for downstream APIs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication:Authority is missing.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters.ValidateAudience = false;
            });

        services.AddAuthorization();

        return services;
    }
}