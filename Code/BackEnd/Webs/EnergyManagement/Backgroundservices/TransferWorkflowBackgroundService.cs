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

            var pollIntervalSeconds = options.PollIntervalSeconds > 0
                ? options.PollIntervalSeconds
                : 60;

            _logger.LogInformation(
                "Transfer scheduler options: Enabled={Enabled}, PollIntervalSeconds={PollIntervalSeconds}",
                options.Enabled,
                pollIntervalSeconds);

            if (!options.Enabled)
            {
                _logger.LogInformation("Transfer scheduler worker disabled. Sleeping 1 minute.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<VnmDbContext>();
                var workflowService = scope.ServiceProvider.GetRequiredService<ITransferWorkflowScheduledService>();

                var nowUtc = DateTime.UtcNow;

                _logger.LogInformation("Transfer scheduler tick at {NowUtc}", nowUtc);

                var dueSchedules = await db.SourceTransferSchedules
                    .Include(x => x.SourceTransferPolicy)
                    .Where(x =>
                        x.IsEnabled &&
                        x.SourceTransferPolicy.IsEnabled &&
                        x.StartDateUtc <= nowUtc &&
                        (!x.EndDateUtc.HasValue || x.EndDateUtc >= nowUtc) &&
                        (
                            !x.NextRunUtc.HasValue ||
                            x.NextRunUtc <= nowUtc
                        ))
                    .OrderBy(x => x.NextRunUtc ?? x.StartDateUtc)
                    .ToListAsync(stoppingToken);

                _logger.LogInformation(
                    "Found {Count} due transfer schedules at {NowUtc}",
                    dueSchedules.Count,
                    nowUtc);

                foreach (var schedule in dueSchedules)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Processing schedule {ScheduleId}: PolicyId={PolicyId}, Source={SourceAddressId}, NextRunUtc={NextRunUtc}, LastRunUtc={LastRunUtc}, Repeat={RepeatEveryValue} {RepeatEveryUnit}, ExecutionMode={ExecutionMode}",
                            schedule.Id,
                            schedule.SourceTransferPolicyId,
                            schedule.SourceTransferPolicy.SourceAddressId,
                            schedule.NextRunUtc,
                            schedule.LastRunUtc,
                            schedule.RepeatEveryValue,
                            schedule.RepeatEveryUnit,
                            schedule.ExecutionMode);

                        var day = DateOnly.FromDateTime(nowUtc);

                        var created = await workflowService.RunAutomaticWorkflowForSourceAsync(
                            schedule.SourceTransferPolicy.SourceAddressId,
                            day,
                            stoppingToken);

                        schedule.LastRunUtc = nowUtc;
                        schedule.NextRunUtc = CalculateNextRunUtc(schedule, nowUtc);

                        _logger.LogInformation(
                            "Processed schedule {ScheduleId}. Source={SourceAddressId}, Day={Day}, Created={CreatedCount}, LastRunUtc={LastRunUtc}, NextRunUtc={NextRunUtc}",
                            schedule.Id,
                            schedule.SourceTransferPolicy.SourceAddressId,
                            day,
                            created.Count,
                            schedule.LastRunUtc,
                            schedule.NextRunUtc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error processing schedule {ScheduleId} for policy {PolicyId}.",
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
        return schedule.ScheduleTypeEnum switch
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

        var candidate = referenceUtc.Date.Add(timeOfDay);

        if (candidate <= referenceUtc)
            candidate = candidate.AddDays(1);

        return candidate;
    }

    private static DateTime? CalculateWeeklyNextRunUtc(SourceTransferSchedule schedule, DateTime referenceUtc)
    {
        if (!schedule.DayOfWeek.HasValue)
            return null;

        var timeOfDay = schedule.TimeOfDayUtc ?? TimeSpan.Zero;
        var candidate = referenceUtc.Date.Add(timeOfDay);

        while ((int)candidate.DayOfWeek != schedule.DayOfWeek.Value || candidate <= referenceUtc)
        {
            candidate = candidate.AddDays(1).Date.Add(timeOfDay);
        }

        return candidate;
    }

    private static DateTime? CalculateMonthlyNextRunUtc(SourceTransferSchedule schedule, DateTime referenceUtc)
    {
        if (!schedule.DayOfMonth.HasValue)
            return null;

        var timeOfDay = schedule.TimeOfDayUtc ?? TimeSpan.Zero;

        var candidate = BuildMonthlyCandidate(
            referenceUtc.Year,
            referenceUtc.Month,
            schedule.DayOfMonth.Value,
            timeOfDay);

        if (candidate <= referenceUtc)
        {
            var nextMonth = referenceUtc.AddMonths(1);
            candidate = BuildMonthlyCandidate(
                nextMonth.Year,
                nextMonth.Month,
                schedule.DayOfMonth.Value,
                timeOfDay);
        }

        return candidate;
    }

    private static DateTime BuildMonthlyCandidate(
        int year,
        int month,
        int requestedDay,
        TimeSpan timeOfDay)
    {
        var day = Math.Min(requestedDay, DateTime.DaysInMonth(year, month));
        return new DateTime(year, month, day).Add(timeOfDay);
    }
}