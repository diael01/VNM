using Infrastructure.Enums;
using Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Repositories.Models;

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
        _logger.LogInformation("Transfer scheduler background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _optionsMonitor.CurrentValue;

            if (!options.Enabled)
            {
                _logger.LogInformation("Transfer scheduler worker disabled. Sleeping 1 minute.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            var pollIntervalSeconds = options.PollIntervalSeconds > 0
                ? options.PollIntervalSeconds
                : 60;

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<VnmDbContext>();
                var workflowService = scope.ServiceProvider.GetRequiredService<ITransferWorkflowService>();

                var nowUtc = DateTime.UtcNow;
                _logger.LogInformation("Scheduler tick at {NowUtc}", nowUtc);
                var dueSchedules = await db.SourceTransferSchedules
                    .Include(x => x.SourceTransferPolicy)
                    .Where(x =>
                        x.IsEnabled &&
                        x.SourceTransferPolicy.IsEnabled &&
                        x.StartDateUtc <= nowUtc &&
                        (!x.EndDateUtc.HasValue || x.EndDateUtc >= nowUtc) &&
                        x.NextRunUtc.HasValue &&
                        x.NextRunUtc <= nowUtc)
                    .OrderBy(x => x.NextRunUtc)
                    .ToListAsync(stoppingToken);
                    _logger.LogInformation("Found {Count} due schedules at {NowUtc}",dueSchedules.Count,nowUtc);

                if (dueSchedules.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), stoppingToken);
                    continue;
                }

                foreach (var schedule in dueSchedules)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Schedule {ScheduleId}: Source={Source}, NextRunUtc={NextRun}, Repeat={Value} {Unit}, Enabled={Enabled}",
                            schedule.Id, schedule.SourceTransferPolicy.SourceAddressId, schedule.NextRunUtc, schedule.RepeatEveryValue, schedule.RepeatEveryUnit, schedule.IsEnabled);

                        var day = DateOnly.FromDateTime(nowUtc);
                        _logger.LogInformation("Calling workflow service for Source={Source} Day={Day}", schedule.SourceTransferPolicy.SourceAddressId, day);
                        var created = await workflowService.RunAutomaticWorkflowForSourceAsync(
                            schedule.SourceTransferPolicy.SourceAddressId,
                            day,
                            stoppingToken);
                        _logger.LogInformation("Workflow service returned {Count} rows for Source={Source}", created.Count, schedule.SourceTransferPolicy.SourceAddressId);
                        schedule.LastRunUtc = nowUtc;
                        schedule.NextRunUtc = CalculateNextRunUtc(schedule, nowUtc);

                        _logger.LogInformation(
                            "Processed schedule {ScheduleId} for source {SourceAddressId}. Created {Count} workflows. ExecutionMode={ExecutionMode}",
                            schedule.Id,
                            schedule.SourceTransferPolicy.SourceAddressId,
                            created.Count,
                            schedule.ExecutionMode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error processing schedule {ScheduleId} for source policy {PolicyId}.",
                            schedule.Id,
                            schedule.SourceTransferPolicyId);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running transfer scheduler worker.");
            }

            await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), stoppingToken);
        }
    }

    private static DateTime? CalculateNextRunUtc(SourceTransferSchedule schedule, DateTime referenceUtc)
    {
        var scheduleType = schedule.ScheduleTypeEnum;

        return scheduleType switch
        {
            ScheduleType.Once => null,

            ScheduleType.Interval => CalculateIntervalNextRunUtc(schedule, referenceUtc),

            ScheduleType.Daily => CalculateDailyNextRunUtc(schedule, referenceUtc),

            ScheduleType.Weekly => CalculateWeeklyNextRunUtc(schedule, referenceUtc),

            ScheduleType.Monthly => CalculateMonthlyNextRunUtc(schedule, referenceUtc),

            _ => null
        };
    }

    private static DateTime? CalculateIntervalNextRunUtc(SourceTransferSchedule schedule, DateTime referenceUtc)
    {
        if (!schedule.RepeatEveryValue.HasValue || schedule.RepeatEveryValue.Value <= 0)
            return null;

        var unit = schedule.RepeatEveryUnitEnum ?? RepeatEveryUnit.Minutes;

        return unit switch
        {
            RepeatEveryUnit.Minutes => referenceUtc.AddMinutes(schedule.RepeatEveryValue.Value),
            RepeatEveryUnit.Hours => referenceUtc.AddHours(schedule.RepeatEveryValue.Value),
            _ => referenceUtc.AddMinutes(schedule.RepeatEveryValue.Value)
        };
    }

    private static DateTime? CalculateDailyNextRunUtc(SourceTransferSchedule schedule, DateTime referenceUtc)
    {
        var timeOfDay = schedule.TimeOfDayUtc ?? TimeSpan.Zero;
        var candidate = referenceUtc.Date.AddDays(1).Add(timeOfDay);
        return candidate;
    }

    private static DateTime? CalculateWeeklyNextRunUtc(SourceTransferSchedule schedule, DateTime referenceUtc)
    {
        if (!schedule.DayOfWeek.HasValue)
            return null;

        var timeOfDay = schedule.TimeOfDayUtc ?? TimeSpan.Zero;
        var current = referenceUtc.Date.AddDays(1);

        while ((int)current.DayOfWeek != schedule.DayOfWeek.Value)
            current = current.AddDays(1);

        return current.Add(timeOfDay);
    }

    private static DateTime? CalculateMonthlyNextRunUtc(SourceTransferSchedule schedule, DateTime referenceUtc)
    {
        if (!schedule.DayOfMonth.HasValue)
            return null;

        var timeOfDay = schedule.TimeOfDayUtc ?? TimeSpan.Zero;

        var nextMonth = new DateTime(referenceUtc.Year, referenceUtc.Month, 1).AddMonths(1);
        var day = Math.Min(schedule.DayOfMonth.Value, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));

        return new DateTime(nextMonth.Year, nextMonth.Month, day).Add(timeOfDay);
    }
}