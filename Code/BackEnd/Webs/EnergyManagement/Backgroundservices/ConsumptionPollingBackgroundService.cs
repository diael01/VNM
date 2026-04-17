using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Repositories.Models;
using Polling.Services.Auth;
using Infrastructure.Options;


namespace ConsumptionPolling.Services
{
    public class ConsumptionPollingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConsumptionPollingService> _logger;
        private readonly ConsumptionPollingOptions _options;
        private readonly MeteringOptions _meteringOptions;
         private readonly IAccessTokenProvider _accessTokenProvider;

        public ConsumptionPollingService(
            IServiceProvider serviceProvider,
            ILogger<ConsumptionPollingService> logger,
            IOptions<ConsumptionPollingOptions> options,
            IOptions<MeteringOptions> meteringOptions,
                IAccessTokenProvider accessTokenProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
            _meteringOptions = meteringOptions.Value;
            _accessTokenProvider = accessTokenProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
			{
				_logger.LogInformation("DailyBalanceComputationBackgroundService is disabled in configuration.");
				return;
			}
            _logger.LogInformation("Consumption polling service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var pollerFactory = scope.ServiceProvider.GetRequiredService<IConsumptionPollerFactory>();
                var poller = pollerFactory.Create(_options);
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<VnmDbContext>>();

                try
                {
                    await using var dbContext = await dbFactory.CreateDbContextAsync(stoppingToken);
                    var reading = await poller.PollAsync(stoppingToken);

                    if (reading != null)
                    {
                        var addressExists = await dbContext.Addresses
                            .AsNoTracking()
                            .AnyAsync(a => a.Id == reading.AddressId, stoppingToken);

                        if (!addressExists)
                        {
                            _logger.LogWarning(
                                "Skipping consumption reading for unknown address {AddressId}. Source={Source}, Timestamp={Timestamp}",
                                reading.AddressId,
                                reading.Source,
                                reading.Timestamp);
                            continue;
                        }

                        var totalReadings = await dbContext.ConsumptionReadings.CountAsync(stoppingToken);
                        if (totalReadings >= 10) // retention cap for demo
                        {
                            await dbContext.ConsumptionReadings.ExecuteDeleteAsync(stoppingToken);
                            _logger.LogWarning("ConsumptionReadings reached retention cap (10). Cleared table.");
                        }

                        dbContext.ConsumptionReadings.Add(reading);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation(
                            "Saved consumption reading: {Power} at {Timestamp}",
                            reading.Power, reading.Timestamp);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error polling consumption meter");
                }
                await Task.Delay(TimeSpan.FromMinutes(_meteringOptions.ReadingIntervalMinutes), stoppingToken);
            }
        }
    }
}
