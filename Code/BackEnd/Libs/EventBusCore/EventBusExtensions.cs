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
                    var rabbitOptions = ResolveRabbitMqOptions(configuration);
                    var queueName = configuration["EventBus:QueueName"];
                    if (string.IsNullOrWhiteSpace(rabbitOptions.Password))
                    {
                        throw new InvalidOperationException(
                            "RabbitMQ password is missing. Set RabbitMQ:Password or provide an Aspire RabbitMQ connection string.");
                    }

                    cfg.Host(
                        rabbitOptions.Host,
                        rabbitOptions.VirtualHost,
                        h =>
                        {
                            h.Username(rabbitOptions.Username);
                            h.Password(rabbitOptions.Password);
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

                    // 6️⃣ Endpoint topology
                    // If a queue name is explicitly configured, bind all service consumers to that queue.
                    // Otherwise fall back to MassTransit auto-derived endpoint names.
                    if (!string.IsNullOrWhiteSpace(queueName))
                    {
                        cfg.ReceiveEndpoint(queueName, endpoint =>
                        {
                            endpoint.ConfigureConsumers(context);
                        });
                    }
                    else
                    {
                        cfg.ConfigureEndpoints(context);
                    }
                });
            });

            return services;
        }

        private static (string Host, string VirtualHost, string Username, string Password) ResolveRabbitMqOptions(IConfiguration configuration)
        {
            // Prefer explicit RabbitMQ section when provided.
            var sectionHost = configuration["RabbitMQ:Host"];
            var sectionVirtualHost = configuration["RabbitMQ:VirtualHost"];
            var sectionUsername = configuration["RabbitMQ:Username"];
            var sectionPassword = configuration["RabbitMQ:Password"];

            if (!string.IsNullOrWhiteSpace(sectionPassword))
            {
                return (
                    Host: string.IsNullOrWhiteSpace(sectionHost) ? "rabbitmq" : sectionHost,
                    VirtualHost: string.IsNullOrWhiteSpace(sectionVirtualHost) ? "/" : sectionVirtualHost,
                    Username: string.IsNullOrWhiteSpace(sectionUsername) ? "guest" : sectionUsername,
                    Password: sectionPassword);
            }

            // Fallback to Aspire-provided connection strings (resource-scoped first).
            var connectionString = configuration["ConnectionStrings:res08-rabbitmq"]
                ?? configuration["ConnectionStrings:rabbitmq"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return (
                    Host: string.IsNullOrWhiteSpace(sectionHost) ? "rabbitmq" : sectionHost,
                    VirtualHost: string.IsNullOrWhiteSpace(sectionVirtualHost) ? "/" : sectionVirtualHost,
                    Username: string.IsNullOrWhiteSpace(sectionUsername) ? "guest" : sectionUsername,
                    Password: sectionPassword ?? string.Empty);
            }

            if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                return (
                    Host: string.IsNullOrWhiteSpace(sectionHost) ? "rabbitmq" : sectionHost,
                    VirtualHost: string.IsNullOrWhiteSpace(sectionVirtualHost) ? "/" : sectionVirtualHost,
                    Username: string.IsNullOrWhiteSpace(sectionUsername) ? "guest" : sectionUsername,
                    Password: sectionPassword ?? string.Empty);
            }

            var userInfoParts = uri.UserInfo.Split(':', 2, StringSplitOptions.None);
            var parsedUsername = userInfoParts.Length > 0 ? Uri.UnescapeDataString(userInfoParts[0]) : string.Empty;
            var parsedPassword = userInfoParts.Length > 1 ? Uri.UnescapeDataString(userInfoParts[1]) : string.Empty;
            var parsedVhost = uri.AbsolutePath;
            if (string.IsNullOrWhiteSpace(parsedVhost) || parsedVhost == "/")
            {
                parsedVhost = "/";
            }
            else
            {
                parsedVhost = Uri.UnescapeDataString(parsedVhost);
            }

            return (
                Host: string.IsNullOrWhiteSpace(uri.Host) ? "rabbitmq" : uri.Host,
                VirtualHost: string.IsNullOrWhiteSpace(parsedVhost) ? "/" : parsedVhost,
                Username: string.IsNullOrWhiteSpace(parsedUsername)
                    ? (string.IsNullOrWhiteSpace(sectionUsername) ? "guest" : sectionUsername)
                    : parsedUsername,
                Password: string.IsNullOrWhiteSpace(parsedPassword) ? (sectionPassword ?? string.Empty) : parsedPassword);
        }
    }
}
