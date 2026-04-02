using Microsoft.Extensions.Options;
using EnergyManagement.Services.Analytics;

namespace EnergyManagement.Services.Transfers
{

	public class DailyBalanceComputationBackgroundService : BackgroundService
	{
		  private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly ILogger<DailyBalanceComputationBackgroundService> _logger;
		private readonly DailyBalanceComputationOptions _options;

		public DailyBalanceComputationBackgroundService(
			   IServiceScopeFactory serviceScopeFactory,
			    IOptions<DailyBalanceComputationOptions> options,
			    ILogger<DailyBalanceComputationBackgroundService> logger)
		{
			   _serviceScopeFactory = serviceScopeFactory;
			    _logger = logger;
			    _options = options.Value;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			if (!_options.Enabled)
			{
				_logger.LogInformation("DailyBalanceComputationBackgroundService is disabled in configuration.");
				return;
			}

            if (_options.IntervalMinutes <= 0)
            {
                _logger.LogWarning(
                    "DailyBalanceComputationBackgroundService has invalid IntervalMinutes={IntervalMinutes}. Using fallback value 5.",
                    _options.IntervalMinutes);
            }
            var intervalMinutes = _options.IntervalMinutes > 0
                ? _options.IntervalMinutes
                : 5;

            var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

            _logger.LogInformation(
                "DailyBalanceComputationBackgroundService started. Interval: {IntervalMinutes} minutes.",
                intervalMinutes);
                _logger.LogInformation("DailyBalanceComputationBackgroundService started with interval {Interval} minutes.", _options.IntervalMinutes);


            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var svc = scope.ServiceProvider.GetRequiredService<IDailyBalanceCalculationService>();
                    var today = DateOnly.FromDateTime(DateTime.Now);
                    await svc.CalculateDailyBalancesForAllInvertersAsync(today, stoppingToken);
                    _logger.LogInformation("Daily balance calculation completed successfully at {Time}.", DateTime.Now);
                }
            }
                catch (OperationCanceledException)
            {
                _logger.LogInformation("Daily balance calculation was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during daily balance calculation.");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
			
		}
	}
}
