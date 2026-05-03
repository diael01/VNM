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

                    var expirationThreshold = DateTime.UtcNow.AddDays(-_options.ExpirationDays);

                    var oldApprovedWorkflows = await dbContext.TransferWorkflows
                        .Where(w => w.Status == (int)TransferStatus.Approved
                                 && w.CreatedAtUtc < expirationThreshold)
                        .ToListAsync(stoppingToken);

                    foreach (var wf in oldApprovedWorkflows)
                    {
                        var oldStatus = wf.TransferStatusEnum;

                        wf.TransferStatusEnum = TransferStatus.Cancelled;

                        dbContext.TransferWorkflowStatusHistory.Add(new TransferWorkflowStatusHistory
                        {
                            TransferWorkflowId = wf.Id,
                            FromStatusEnum = oldStatus,
                            ToStatusEnum = TransferStatus.Cancelled,
                            UpdatedAtUtc = DateTime.UtcNow,
                            UpdatedBy = "System",
                            Note = $"Auto-cancelled after {_options.ExpirationDays} days"
                        });
                    }

                    if (oldApprovedWorkflows.Any())
                    {
                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Cleaned up {Count} old workflows", oldApprovedWorkflows.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during TransferWorkflow cleanup");
                }

                await Task.Delay(TimeSpan.FromMinutes(_options.RunIntervalMinutes), stoppingToken);
            }
        }
    }
}