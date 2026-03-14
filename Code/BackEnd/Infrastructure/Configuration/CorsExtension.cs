using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VNM.Infrastructure.Configuration;

namespace VNM.Infrastructure.Extensions;

/// <summary>
/// Provides reusable CORS registrations.
/// </summary>
public static class CorsExtensions
{
    public const string FrontendCorsPolicy = "FrontendCorsPolicy";

    /// <summary>
    /// Registers frontend CORS policy from configuration.
    /// </summary>
    public static IServiceCollection AddFrontendCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FrontendOptions>(
            configuration.GetSection("Frontend"));

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var frontendOptions = serviceProvider
                    .GetRequiredService<IOptions<FrontendOptions>>()
                    .Value;

                if (string.IsNullOrWhiteSpace(frontendOptions.BaseUrl))
                {
                    throw new InvalidOperationException("Frontend:BaseUrl is missing.");
                }

                policy
                    .WithOrigins(frontendOptions.BaseUrl)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}