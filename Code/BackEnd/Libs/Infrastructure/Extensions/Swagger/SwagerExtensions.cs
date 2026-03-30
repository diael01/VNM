using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace VNM.Infrastructure.Extensions;

public static class SwagerExtensions
{
    private const string SwaggerDocVersion = "v1";
    private const string SwaggerEndpointPath = "/swagger/v1/swagger.json";

    public static IServiceCollection AddSwager(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
            options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
        });
        return services;
    }

    public static WebApplication UseSwagerInDevelopment(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                SwaggerEndpointPath,
                $"{app.Environment.ApplicationName} {SwaggerDocVersion.ToUpperInvariant()}");
            options.RoutePrefix = string.Empty;
        });

        return app;
    }
}