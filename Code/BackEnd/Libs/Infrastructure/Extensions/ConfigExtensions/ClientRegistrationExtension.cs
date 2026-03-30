using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VNM.Infrastructure.Configuration;

namespace VNM.Infrastructure.Extensions;

/// <summary>
/// Provides reusable downstream HttpClient registrations.
/// </summary>
public static class ClientRegistrationExtension
{
    /// <summary>
    /// Registers downstream service endpoints from configuration.
    /// </summary>
    public static IServiceCollection AddDownstreamServiceClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EndpointsOptions>(
            configuration.GetSection("ServiceEndpoints"));

        services.AddHttpClient("inverter-api", (serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<EndpointsOptions>>()
                .Value;

            if (string.IsNullOrWhiteSpace(options.InverterApi))
            {
                throw new InvalidOperationException("ServiceEndpoints:InverterApi is missing.");
            }

            client.BaseAddress = new Uri(options.InverterApi);
        });

        services.AddHttpClient("meter-api", (serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<EndpointsOptions>>()
                .Value;

            if (string.IsNullOrWhiteSpace(options.MeterApi))
            {
                throw new InvalidOperationException("ServiceEndpoints:MeterApi is missing.");
            }

            client.BaseAddress = new Uri(options.MeterApi);
        });

        return services;
    }
}