
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Repositories.Models;
using Infrastructure.Options;

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
        private readonly MeteringOptions _meteringOptions;

        public InverterPollingService(
            IServiceProvider serviceProvider,
            ILogger<InverterPollingService> logger,
            IOptions<InverterPollingOptions> options,
            IOptions<MeteringOptions> meteringOptions)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
            _meteringOptions = meteringOptions.Value;   
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
			{
				_logger.LogInformation("DailyBalanceComputationBackgroundService is disabled in configuration.");
				return;
			}
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
                        if (totalReadings >= 10)
                        {
                            await dbContext.InverterReadings.ExecuteDeleteAsync(stoppingToken);
                            
                        }

                        var addressId = await dbContext.InverterInfos
                            .Where(x => x.Id == reading.InverterInfoId)
                            .Select(x => x.AddressId)
                            .SingleAsync(stoppingToken);

                        if(addressId != reading.AddressId)                        
                            _logger.LogWarning("AddressId from reading ({ReadingAddressId}) does not match AddressId from InverterInfo ({InverterInfoAddressId}). Using InverterInfo AddressId.", reading.AddressId, addressId);                        
                        reading.AddressId = addressId;
                        reading.Source = _options.Source;

                        dbContext.InverterReadings.Add(reading);
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
                await Task.Delay(TimeSpan.FromMinutes(_meteringOptions.ReadingIntervalMinutes), stoppingToken);
            }
        }
    }
}
