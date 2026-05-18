using Infrastructure.Enums;
using Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Repositories.Models;

namespace EnergyManagement.Services.Transfers
{

    public class TransferWorkflowCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TransferWorkflowCleanupBackgroundService> _logger;
        private readonly TransferWorkflowCleanupOptions _options;

        public TransferWorkflowCleanupBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<TransferWorkflowCleanupOptions> options,
            ILogger<TransferWorkflowCleanupBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TransferWorkflowCleanupBackgroundService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<VnmDbContext>();

                    var todayStartUtc = DateTime.UtcNow.Date;

                    var expiredWorkflows = await dbContext.TransferWorkflows
                        .Where(w =>
                            w.BalanceDayUtc < todayStartUtc &&
                            (w.Status == (int)TransferStatus.Planned || w.Status == (int)TransferStatus.Approved))
                        .ToListAsync(stoppingToken);

                    foreach (var wf in expiredWorkflows)
                    {
                        var oldStatus = wf.TransferStatusEnum;
                        var note = BuildExpiredNote(oldStatus);

                        wf.TransferStatusEnum = TransferStatus.Discontinued;
                        wf.Notes = note;
                        wf.UpdatedAtUtc = DateTime.UtcNow;
                        wf.UpdatedBy = "system";

                        dbContext.TransferWorkflowStatusHistory.Add(new TransferWorkflowStatusHistory
                        {
                            TransferWorkflowId = wf.Id,
                            FromStatusEnum = oldStatus,
                            ToStatusEnum = TransferStatus.Discontinued,
                            UpdatedAtUtc = DateTime.UtcNow,
                            UpdatedBy = "System",
                            Note = note
                        });
                    }

                    if (expiredWorkflows.Any())
                    {
                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Discontinued {Count} expired workflows from previous days", expiredWorkflows.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during TransferWorkflow cleanup");
                }

                await Task.Delay(TimeSpan.FromMinutes(_options.RunIntervalMinutes), stoppingToken);
            }
        }

        private static string BuildExpiredNote(TransferStatus fromStatus)
        {
            var reason = fromStatus switch
            {
                TransferStatus.Planned => DiscontinuedReason.ExpiredBeforeApproval,
                TransferStatus.Approved => DiscontinuedReason.ExpiredBeforeExecution,
                _ => DiscontinuedReason.ExpiredBeforeExecution
            };

            return reason switch
            {
                DiscontinuedReason.ExpiredBeforeApproval => "Expired: workflow was still Planned and was not approved/executed before the day changed",
                DiscontinuedReason.ExpiredBeforeExecution => "Expired: workflow was Approved but not executed before the day changed",
                _ => "Expired: workflow was not completed before the day changed"
            };
        }
    }
}
