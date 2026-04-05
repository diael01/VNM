using Microsoft.Extensions.DependencyInjection; // <-- this is required
using Microsoft.Extensions.Options;
using InverterPolling.Services;
using Infrastructure.Options;

namespace Infrastructure.Polling;

public static class InverterPollingServiceCollectionExtensions
{
    public static IServiceCollection AddInverterPolling(this IServiceCollection services)
    {

        services.AddScoped<IInverterPollerFactory, InverterPollerFactory>();
        services.AddScoped<IInverterPoller>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<InverterPollingOptions>>().Value;
            var factory = sp.GetRequiredService<IInverterPollerFactory>();
            return factory.Create(options);
        });

        services.AddHostedService<InverterPollingService>();
        return services;
    }
}
