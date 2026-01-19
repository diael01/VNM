using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using EventBusTestHarness.Consumers;

namespace EventBusTestHarness
{
    public static class TestBusExtensions
    {
        public static IHostBuilder AddTestEventBus(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                var resourceBuilder = ResourceBuilder.CreateDefault()
                    .AddService("EventBusTestHarness", "1.0.0");

                // OpenTelemetry
                services.AddOpenTelemetry()
                    .WithTracing(tracing =>
                    {
                        tracing
                            .SetResourceBuilder(resourceBuilder)
                            .AddMassTransitInstrumentation()
                            .AddConsoleExporter(); // Optional, can be OTLP
                    })
                    .WithMetrics(metrics =>
                    {
                        metrics
                            .SetResourceBuilder(resourceBuilder)
                            .AddMassTransitInstrumentation()
                            .AddConsoleExporter();
                    });

                // MassTransit Test Harness
                services.AddMassTransitTestHarness(cfg =>
                {
                    cfg.AddConsumer<MeterIngestionConsumer>();
                    cfg.AddConsumer<DashboardConsumer>();

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
