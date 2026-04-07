using Microsoft.Extensions.Options;
using EnergyManagement.Services.Analytics;
using Infrastructure.Options;

public class DailyBalanceComputationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyBalanceComputationBackgroundService> _logger;
    private readonly DailyBalanceComputationOptions _options;

    public DailyBalanceComputationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<DailyBalanceComputationOptions> options,
        ILogger<DailyBalanceComputationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Daily balance computation disabled.");
            return;
        }

        var intervalMinutes = _options.IntervalMinutes > 0
            ? _options.IntervalMinutes
            : 1;

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        _logger.LogInformation("Daily balance service started. Interval: {Interval} min", intervalMinutes);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();

                var svc = scope.ServiceProvider
                    .GetRequiredService<IDailyBalanceCalculationService>();

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var balances = await svc.CalculateDailyBalancesForAllAddressesAsync(today, stoppingToken);

                _logger.LogInformation(
                    "Daily balance calculated at {Time}. Addresses processed: {Count}",
                    DateTime.UtcNow,
                    balances.Count);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Daily balance service stopped.");
        }
    }
}