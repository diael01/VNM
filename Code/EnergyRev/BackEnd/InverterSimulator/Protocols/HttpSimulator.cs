using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InverterSimulator.Protocols.Http
{
    // Configuration POCO, still can be bound from appsettings.json
    public class HttpSimulatorConfig
    {
        public int Port { get; set; } = 5000;
        public int MinPower { get; set; } = 0;
        public int MaxPower { get; set; } = 5000;
        public int MinVoltage { get; set; } = 200;
        public int MaxVoltage { get; set; } = 250;
        public int MinCurrent { get; set; } = 0;
        public int MaxCurrent { get; set; } = 20;
    }

    // Hosted service that runs the simulator
    public class HttpSimulator : IHostedService
    {
        private readonly HttpSimulatorConfig _config;
        private readonly Random _rand = new Random();
        private IHost? _webHost;

        public HttpSimulator(IOptions<HttpSimulatorConfig> options)
        {
            _config = options.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var builder = WebApplication.CreateBuilder();

            // Use the port from config
            builder.WebHost.UseUrls($"http://*:{_config.Port}");

            var app = builder.Build();

            app.MapGet("/inverter/data", () =>
            {
                var data = new
                {
                    PowerW = _rand.Next(_config.MinPower, _config.MaxPower + 1),
                    VoltageV = _rand.Next(_config.MinVoltage, _config.MaxVoltage + 1),
                    CurrentA = _rand.Next(_config.MinCurrent, _config.MaxCurrent + 1),
                    Timestamp = DateTime.UtcNow
                };

                return Results.Json(data);
            });

            Console.WriteLine($"HTTP simulator running on port {_config.Port}, GET /inverter/data");

            // Store reference to host for graceful stop
            _webHost = app;

            await app.RunAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Gracefully stop the web host
            if (_webHost != null)
            {
                Console.WriteLine("Stopping HTTP simulator...");
                return _webHost.StopAsync(cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}
