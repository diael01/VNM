using Microsoft.AspNetCore.Builder;

namespace VNM.Infrastructure.Extensions;


/// <summary>
/// Provides application builder extensions for security middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds authentication and authorization middleware to the pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder instance.</returns>
    public static IApplicationBuilder UseBffSecurity(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}