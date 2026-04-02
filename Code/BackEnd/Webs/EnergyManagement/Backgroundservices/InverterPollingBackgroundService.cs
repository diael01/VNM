
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Repositories.Models;

namespace InverterPolling.Services
{
    /// <summary>
    /// Protocol-agnostic inverter polling service.
    /// </summary>
    public class InverterPollingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InverterPollingService> _logger;
        private readonly InverterPollingOptions _options;

        public InverterPollingService(
            IServiceProvider serviceProvider,
            ILogger<InverterPollingService> logger,
            IOptions<InverterPollingOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Inverter polling service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var poller = scope.ServiceProvider.GetRequiredService<IInverterPoller>();
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<VnmDbContext>>();

                try
                {
                    await using var dbContext = await dbFactory.CreateDbContextAsync(stoppingToken);
                    var reading = await poller.PollAsync(stoppingToken);

                    if (reading != null)
                    {
                        var totalReadings = await dbContext.InverterReadings.CountAsync(stoppingToken);
                        if (totalReadings >= 10) //todo: remove this when is stable
                        {
                            await dbContext.InverterReadings.ExecuteDeleteAsync(stoppingToken);
                            _logger.LogWarning("InverterReadings reached retention cap (10). Cleared table.");
                        }

                        var entity = new InverterReading
                        {
                            Timestamp = reading.Timestamp,
                            Power = reading.Power,
                            Voltage = reading.Voltage,
                            Current = reading.Current,
                            Source = _options.Source,
                        };

                        dbContext.InverterReadings.Add(entity);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation(
                            "Saved inverter reading: {Power} W, {Voltage} V, {Current} A at {Timestamp}",
                            reading.Power, reading.Voltage, reading.Current, reading.Timestamp);
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
