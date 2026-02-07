using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using InverterPolling.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace InverterPolling.Services
{
    // Configurable options for polling
    public class InverterPollingOptions
    {
        public int PollIntervalMinutes { get; set; } = 1440; // default 24 hours
        public string Source { get; set; } = "Simulator";
    }

    /// <summary>
    /// Protocol-agnostic inverter polling service.
    /// </summary>
    public class InverterPollingService : BackgroundService
    {
        private readonly ILogger<InverterPollingService> _logger;
        private readonly IInverterPoller _poller;
        private readonly InverterPollingOptions _options;
        private readonly IServiceProvider _serviceProvider;

        public InverterPollingService(
            ILogger<InverterPollingService> logger,
            IInverterPoller poller,
            IOptions<InverterPollingOptions> options,
            IServiceProvider serviceProvider) // <-- inject IServiceProvider
        {
            _logger = logger;
            _poller = poller;
            _options = options.Value;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Inverter polling service started using {PollerType}", _poller.GetType().Name);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Create a scoped DbContext per polling cycle
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<SolarDbContext>();

                    var reading = await _poller.PollAsync(stoppingToken);

                    if (reading != null)
                    {
                        reading.Source = _options.Source;

                        dbContext.InverterReadings.Add(reading);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation(
                            "Saved inverter reading: {PowerW} W, {VoltageV} V, {CurrentA} A at {Timestamp}",
                            reading.PowerW, reading.VoltageV, reading.CurrentA, reading.TimestampUtc);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error polling inverter");
                }

                await Task.Delay(TimeSpan.FromMinutes(_options.PollIntervalMinutes), stoppingToken);
            }
        }
    }
}
