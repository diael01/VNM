using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Models;


namespace InverterPolling.Services
{
    /// <summary>
    /// Protocol-agnostic inverter polling service.
    /// </summary>
    public class InverterPollingService : BackgroundService
    {
        private readonly ILogger<InverterPollingService> _logger;
        private readonly IInverterPoller _poller;
        private readonly InverterPollingOptions _options;
        private readonly IDbContextFactory<VnmDbContext> _dbFactory;

        public InverterPollingService(
            ILogger<InverterPollingService> logger,
            IInverterPoller poller,
            IOptions<InverterPollingOptions> options,
            IDbContextFactory<VnmDbContext> dbFactory)
        {
            _logger = logger;
            _poller = poller;
            _options = options.Value;
            _dbFactory = dbFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Inverter polling service started using {PollerType}", _poller.GetType().Name);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ✅ Create a new DbContext per polling cycle
                    await using var dbContext = await _dbFactory.CreateDbContextAsync(stoppingToken);

                    var reading = await _poller.PollAsync(stoppingToken);

                    if (reading != null)
                    {
                        var totalReadings = await dbContext.InverterReadings.CountAsync(stoppingToken);
                        if (totalReadings >= 100) //todo: read from appsettings, add a guard, this is only for test
                        {
                            await dbContext.InverterReadings.ExecuteDeleteAsync(stoppingToken);
                            _logger.LogWarning("InverterReadings reached retention cap (100). Cleared table.");
                        }

                        var entity = new Repositories.Models.InverterReading
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
