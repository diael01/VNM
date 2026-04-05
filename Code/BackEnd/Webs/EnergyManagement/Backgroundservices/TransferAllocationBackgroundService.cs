using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EnergyManagement.Services.Transfers;

public class TransferAllocationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransferAllocationBackgroundService> _logger;
    private readonly IOptionsMonitor<TransferAllocationOptions> _optionsMonitor;

    public TransferAllocationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<TransferAllocationOptions> optionsMonitor,
        ILogger<TransferAllocationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Transfer allocation background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _optionsMonitor.CurrentValue;

            if (!options.Enabled)
            {
                _logger.LogInformation("Transfer allocation disabled. Sleeping 1 minute.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            var intervalMinutes = options.IntervalMinutes > 0
                ? options.IntervalMinutes
                : 1;

            using var scope = _scopeFactory.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<ITransferAllocationService>();

            var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);

            try
            {
                var created = await service.RunAutomaticAllocationAsync(todayUtc, stoppingToken);

                _logger.LogInformation(
                    "Automatic transfer allocation completed at {Time}. Created {Count} transfers. Mode={Mode}",
                    DateTime.UtcNow,
                    created.Count,
                    options.DistributionMode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running automatic transfer allocation.");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}