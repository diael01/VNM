using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace EventBusCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventBus(
            this IServiceCollection services,
            IConfiguration configuration,
            params Assembly[] consumerAssemblies)
        {
            services.AddMassTransit(x =>
            {
                // 1️⃣ Clean queue naming
                x.SetKebabCaseEndpointNameFormatter();

                // 2️⃣ Auto-register consumers
                foreach (var assembly in consumerAssemblies)
                {
                    x.AddConsumers(assembly);
                }

                // 3️⃣ Transport + policies
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(
                        configuration["RabbitMQ:Host"] ?? "rabbitmq",
                        configuration["RabbitMQ:VirtualHost"] ?? "/",
                        h =>
                        {
                            h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                            h.Password(configuration["RabbitMQ:Password"] ?? "guest");
                        });

                    // Read retry settings from config
                    int retryCount = int.TryParse(configuration["EventBus:Retry:Count"], out var rc) ? rc : 3;
                    int retryIntervalSec = int.TryParse(configuration["EventBus:Retry:IntervalSeconds"], out var ri) ? ri : 5;

                    cfg.UseMessageRetry(r => r.Interval(retryCount, TimeSpan.FromSeconds(retryIntervalSec)));

                    // Read delayed redelivery settings from config
                    string redeliveryIntervals = configuration["EventBus:Redelivery:IntervalsSeconds"]
                                                 ?? "10,60,300"; // defaults in seconds

                    var intervals = redeliveryIntervals
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => TimeSpan.FromSeconds(int.Parse(s.Trim())))
                        .ToArray();

                    cfg.UseDelayedRedelivery(r => r.Intervals(intervals));

                    // 6️⃣ Auto endpoints with DLQ
                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
