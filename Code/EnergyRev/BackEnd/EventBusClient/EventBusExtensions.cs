using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks; // for AddHealthChecks
using MassTransit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;


namespace EventBusClient
{
    public static class EventBusExtensions
    {
        public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure MassTransit
            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(configuration["RabbitMQ:Host"]);
                    cfg.ConfigureEndpoints(ctx); 
                });
            });

            // Configure OpenTelemetry metrics / traces
            services.AddOpenTelemetry().WithMetrics(m =>
            {
                m.AddMeter("EventBusClient");
            });

            // Health checks
            services.AddHealthChecks()
                .AddCheck<EventBusHealthCheck>("eventbus");

            return services;
        }
    }
}
