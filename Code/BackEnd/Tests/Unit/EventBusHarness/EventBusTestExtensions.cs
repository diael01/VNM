using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

using Dashboard.Consumers;
using MeterIngestion.Consumers;



namespace EventBusTestHarness
{
    public static class EventBusTestExtensions
    {
        public static IHostBuilder AddTestEventBus(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                var resourceBuilder = ResourceBuilder.CreateDefault()
                    .AddService("EventBusTestHarness", "1.0.0");

                // OpenTelemetry: optional but useful for tracing test messages
                services.AddOpenTelemetry()
                    .WithTracing(tracing =>
                    {
                        tracing
                            .SetResourceBuilder(resourceBuilder)
                            .AddConsoleExporter(); // Optional, can switch to OTLP
                    })
                    .WithMetrics(metrics =>
                    {
                        metrics
                            .SetResourceBuilder(resourceBuilder)
                            .AddConsoleExporter();
                    });

                // MassTransit Test Harness
                services.AddMassTransitTestHarness(cfg =>
                {
                    // Auto-register all consumers from the service assemblies
                    cfg.AddConsumers(typeof(MeterIngestion.Consumers.MeterEventConsumer).Assembly);
                    cfg.AddConsumers(typeof(Dashboard.Consumers.DashboardConsumer).Assembly);

                    cfg.UsingInMemory((context, cfgBus) =>
                    {
                        cfgBus.ConfigureEndpoints(context);
                    });
                });
            });

            return hostBuilder;
        }
    }
}
