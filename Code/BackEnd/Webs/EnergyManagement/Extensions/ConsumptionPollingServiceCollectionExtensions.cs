using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ConsumptionPolling.Services;

namespace Infrastructure.Polling;

public static class ConsumptionPollingServiceCollectionExtensions
{
    public static IServiceCollection AddConsumptionPolling(this IServiceCollection services)
    {
        services.AddScoped<IConsumptionPollerFactory, ConsumptionPollerFactory>();
        services.AddScoped<IConsumptionPoller>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ConsumptionPollingOptions>>().Value;
            var factory = sp.GetRequiredService<IConsumptionPollerFactory>();
            return factory.Create(options);
        });

        services.AddHostedService<ConsumptionPollingService>();
        return services;
    }
}
