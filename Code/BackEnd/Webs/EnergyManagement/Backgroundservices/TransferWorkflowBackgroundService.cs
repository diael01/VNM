using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EnergyManagement.Services.Transfers;

public class TransferWorkflowBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransferWorkflowBackgroundService> _logger;
    private readonly IOptionsMonitor<TransferWorkflowOptions> _optionsMonitor;

    public TransferWorkflowBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<TransferWorkflowOptions> optionsMonitor,
        ILogger<TransferWorkflowBackgroundService> logger)
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
                .GetRequiredService<ITransferWorkflowService>();

            var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);

            try
            {
                var created = await service.RunAutomaticWorkflowAsync(todayUtc, stoppingToken);

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
