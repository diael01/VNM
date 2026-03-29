using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using Serilog.Sinks.OpenTelemetry;

namespace ServiceDefaults;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        // TelemetryService will be registered in the API project

        return builder;
    }

    private static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: builder.Environment.ApplicationName,
                         serviceVersion: typeof(Extensions).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                         serviceInstanceId: Environment.MachineName);

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddSource("EnergyManagementService")
                    .AddSource("MultiSiteEnergyService")
                    .AddSource("Simulators");

                // Configure sampling based on environment
                var samplingStrategy = builder.Configuration["OpenTelemetry:Sampling:Strategy"] ?? "default";
                switch (samplingStrategy.ToLower())
                {
                    case "always":
                        tracing.SetSampler(new AlwaysOnSampler());
                        break;
                    case "never":
                        tracing.SetSampler(new AlwaysOffSampler());
                        break;
                    case "custom":
                        tracing.SetSampler(new CustomSampler());
                        break;
                    case "ratio":
                        var ratio = builder.Configuration.GetValue<double>("OpenTelemetry:Sampling:Ratio", 0.1);
                        tracing.SetSampler(new TraceIdRatioBasedSampler(ratio));
                        break;
                    default:
                        // Default to 10% sampling in production, 100% in development
                        var defaultRatio = builder.Environment.IsDevelopment() ? 1.0 : 0.1;
                        tracing.SetSampler(new TraceIdRatioBasedSampler(defaultRatio));
                        break;
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                    .AddMeter("System.Net.Http")
                    .AddMeter("EnergyManagementService")
                    .AddMeter("MultiSiteEnergyService")
                    .AddMeter("Simulators");
            });

        builder.AddOpenTelemetryExporters();
        builder.ConfigureSerilogWithOpenTelemetry();

        return builder;
    }

    private static IHostApplicationBuilder ConfigureSerilogWithOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            var appName = builder.Environment.ApplicationName;
            var logsRoot = builder.Configuration["Logging:File:RootPath"] ?? "Logs";
            var fileLoggingEnabled = builder.Configuration.GetValue<bool?>("Logging:File:Enabled")
                ?? builder.Environment.IsDevelopment();
            var retainedFileCount = builder.Configuration.GetValue<int?>("Logging:File:RetainedFileCountLimit") ?? 3;
            var fileSizeLimitBytes = builder.Configuration.GetValue<long?>("Logging:File:FileSizeLimitBytes") ?? 10_485_760;

            loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationName", appName)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                );

            if (fileLoggingEnabled)
            {
                loggerConfiguration.WriteTo.File(
                    path: Path.Combine(logsRoot, appName, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: retainedFileCount,
                    fileSizeLimitBytes: fileSizeLimitBytes,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(5));
            }

            // Check if OTLP endpoint is configured (Aspire will set this automatically)
            //var otlpEndpoint = !builder.Environment.IsDevelopment() && 
            //     !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
            var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                loggerConfiguration.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otlpEndpoint;
                    options.Protocol = OtlpProtocol.Grpc;
                    options.IncludedData = IncludedData.TraceIdField |
                                          IncludedData.SpanIdField |
                                          IncludedData.MessageTemplateTextAttribute |
                                          IncludedData.SpecRequiredResourceAttributes;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = builder.Environment.ApplicationName,
                        ["service.version"] = typeof(Extensions).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                        ["deployment.environment"] = builder.Environment.EnvironmentName
                    };
                });
            }

            var appInsightsConnectionString =
                builder.Configuration["ApplicationInsights:ConnectionString"]
                ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

            if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
            {
                loggerConfiguration.WriteTo.ApplicationInsights(
                    connectionString: appInsightsConnectionString,
                    telemetryConverter: TelemetryConverter.Traces);
            }

            if (builder.Environment.IsDevelopment())
            {
                loggerConfiguration.MinimumLevel.Debug();
            }
            else
            {
                loggerConfiguration.MinimumLevel.Information();
            }

            loggerConfiguration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information);
            loggerConfiguration.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);
        });

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());
        }

        return builder;
    }

    private static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}

