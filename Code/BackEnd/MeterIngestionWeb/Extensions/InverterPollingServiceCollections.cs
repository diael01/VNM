using Microsoft.Extensions.DependencyInjection; // <-- this is required
using Microsoft.Extensions.Options;
using InverterPolling.Services;
using InverterPolling.Data;

namespace Infrastructure.Polling;

public static class InverterPollingServiceCollectionExtensions
{
    public static IServiceCollection AddInverterPolling(this IServiceCollection services)
    {
        services.AddSingleton<IInverterPollerFactory, InverterPollerFactory>();
        services.AddSingleton<IInverterPoller>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<InverterPollingOptions>>().Value;
            var factory = sp.GetRequiredService<IInverterPollerFactory>();
            return factory.Create(options);
        });

        services.AddHostedService<InverterPollingService>();
        return services;
    }
}
