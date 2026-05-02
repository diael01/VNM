using Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Models;

namespace EnergyManagement.Services.Transfers;

public class TransferWorkflowService : ITransferWorkflowService
{
    private readonly VnmDbContext _db;
    private readonly ILogger<TransferWorkflowService> _logger;

    public TransferWorkflowService(
        VnmDbContext db,
        ILogger<TransferWorkflowService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TransferWorkflow>> RunAutomaticWorkflowForSourceAsync(
        int sourceAddressId,
        DateOnly day,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RunAutomaticWorkflowForSourceAsync START Source={Source} Day={Day}",
            sourceAddressId,
            day);

        var dayStartUtc = ToUtcStartOfDay(day);
        var dayEndUtc = dayStartUtc.AddDays(1);

        // Important rule:
        // As long as all auto rows are still Planned, the scheduler may refresh them.
        // Once the user touched the workflow lifecycle (Approved/Executed/Settled/Rejected/etc.),
        // planning is frozen for that source/day to avoid overwriting business decisions.
        if (await HasNonPlannedAutoWorkflowsForSourceAsync(sourceAddressId, dayStartUtc, dayEndUtc, ct))
        {
            _logger.LogWarning(
                "Skipping automatic workflow refresh for Source={Source} Day={Day} because at least one auto workflow for this source/day is no longer Planned.",
                sourceAddressId,
                day);

            return Array.Empty<TransferWorkflow>();
        }

        var positions = await GetAddressPositionsAsync(dayStartUtc, dayEndUtc, ct);
        var positionsByAddress = positions.ToDictionary(x => x.AddressId);

        _logger.LogInformation("Loaded {Count} positions", positions.Count);

        if (!positionsByAddress.TryGetValue(sourceAddressId, out var sourcePosition))
        {
            _logger.LogWarning("No position found for source {Source}", sourceAddressId);
            return Array.Empty<TransferWorkflow>();
        }

        if (sourcePosition.RemainingSurplusKwh <= 0)
        {
            _logger.LogWarning("Source {Source} has no surplus", sourceAddressId);
            await DeleteExistingAutoPlannedRowsForSourceAsync(sourceAddressId, dayStartUtc, dayEndUtc, ct);
            return Array.Empty<TransferWorkflow>();
        }

        var rules = await _db.DestinationTransferRules
            .Include(x => x.SourceTransferPolicy)
            .Where(x =>
                x.SourceTransferPolicy.SourceAddressId == sourceAddressId &&
                x.SourceTransferPolicy.IsEnabled &&
                x.IsEnabled)
            .ToListAsync(ct);

        _logger.LogInformation("Loaded {Count} rules for source {Source}", rules.Count, sourceAddressId);

        if (rules.Count == 0)
        {
            await DeleteExistingAutoPlannedRowsForSourceAsync(sourceAddressId, dayStartUtc, dayEndUtc, ct);
            return Array.Empty<TransferWorkflow>();
        }

        foreach (var rule in rules.Where(r => r.DestinationAddressId == sourceAddressId))
        {
            _logger.LogWarning(
                "Invalid transfer rule detected. SourceAddressId={SourceAddressId} and DestinationAddressId={DestinationAddressId} are the same. RuleId={RuleId}",
                sourceAddressId,
                rule.DestinationAddressId,
                rule.Id);
        }

        var distributionMode = (TransferDistributionMode)rules.First().SourceTransferPolicy.DistributionMode;

        var destinations = rules
            .Where(r => r.DestinationAddressId != sourceAddressId)
            .Where(r => positionsByAddress.ContainsKey(r.DestinationAddressId))
            .Select(r => new DestinationEntry(r, positionsByAddress[r.DestinationAddressId]))
            .Where(x => x.Position.RemainingDeficitKwh > 0)
            .ToList();

        _logger.LogInformation("Eligible destinations: {Count}", destinations.Count);

        if (destinations.Count == 0)
        {
            await DeleteExistingAutoPlannedRowsForSourceAsync(sourceAddressId, dayStartUtc, dayEndUtc, ct);
            return Array.Empty<TransferWorkflow>();
        }

        var desiredWorkflows = distributionMode switch
        {
            TransferDistributionMode.Fair =>
                AllocateFair(sourcePosition, destinations, dayStartUtc),

            TransferDistributionMode.Priority =>
                AllocatePriority(sourcePosition, destinations, dayStartUtc),

            TransferDistributionMode.Weighted =>
                AllocateWeighted(sourcePosition, destinations, dayStartUtc),

            _ => throw new InvalidOperationException("Unknown distribution mode")
        };

        var refreshed = await UpsertAutoPlannedRowsForSourceAsync(
            sourceAddressId,
            dayStartUtc,
            dayEndUtc,
            desiredWorkflows,
            ct);

        _logger.LogInformation(
            "Automatic workflow refresh finished for Source={Source}, Day={Day}. Desired={DesiredCount}, Returned={ReturnedCount}",
            sourceAddressId,
            day,
            desiredWorkflows.Count,
            refreshed.Count);

        return refreshed;
    }

    public async Task<IReadOnlyList<TransferWorkflow>> RunAutomaticWorkflowAsync(
        DateOnly day,
        CancellationToken ct = default)
    {
        var sourceIds = await _db.SourceTransferPolicies
            .Where(x => x.IsEnabled)
            .Select(x => x.SourceAddressId)
            .Distinct()
            .ToListAsync(ct);

        var allResults = new List<TransferWorkflow>();
        foreach (var sourceId in sourceIds)
        {
            var results = await RunAutomaticWorkflowForSourceAsync(sourceId, day, ct);
            allResults.AddRange(results);
        }

        return allResults;
    }

    public async Task<IReadOnlyList<TransferWorkflow>> ExecuteManualTransferAsync(
        ManualTransferRequest request,
        CancellationToken ct = default)
    {
        if (request.Targets == null || request.Targets.Count == 0)
            return Array.Empty<TransferWorkflow>();

        var dayStartUtc = ToUtcStartOfDay(request.Day);
        var dayEndUtc = dayStartUtc.AddDays(1);

        var positions = await GetAddressPositionsAsync(dayStartUtc, dayEndUtc, ct);
        var positionsByAddress = positions.ToDictionary(x => x.AddressId);

        if (!positionsByAddress.TryGetValue(request.SourceAddressId, out var sourcePosition))
            throw new InvalidOperationException(
                $"Source address {request.SourceAddressId} has no balance position for {request.Day}.");

        // Do not allow a manual workflow to be created/changed after lifecycle action already happened.
        if (await HasNonPlannedManualWorkflowsForSourceAsync(request.SourceAddressId, dayStartUtc, dayEndUtc, ct))
            throw new InvalidOperationException(
                $"Manual workflows for source {request.SourceAddressId} and day {request.Day} already contain non-Planned rows. Manual re-planning is not allowed.");

        var desired = new List<TransferWorkflow>();
        var runningRemainingSource = sourcePosition.RemainingSurplusKwh;

        foreach (var target in request.Targets)
        {
            if (runningRemainingSource <= 0)
                break;

            if (target.RequestedKwh <= 0)
                continue;

            if (target.DestinationAddressId == request.SourceAddressId)
                throw new InvalidOperationException("Source and destination cannot be the same address.");

            if (!positionsByAddress.TryGetValue(target.DestinationAddressId, out var destinationPosition))
                throw new InvalidOperationException(
                    $"Destination address {target.DestinationAddressId} has no balance position for {request.Day}.");

            var sourceAvailableBefore = decimal.Round(runningRemainingSource, 4);
            var destinationNeedBefore = decimal.Round(destinationPosition.RemainingDeficitKwh, 4);

            var amount = Math.Min(target.RequestedKwh, sourceAvailableBefore);
            amount = Math.Min(amount, destinationNeedBefore);
            amount = decimal.Round(amount, 4);

            if (amount <= 0)
                continue;

            var remainingSourceAfter = decimal.Round(sourceAvailableBefore - amount, 4);

            desired.Add(new TransferWorkflow
            {
                SourceAddressId = request.SourceAddressId,
                DestinationAddressId = target.DestinationAddressId,
                SourceSurplusKwhAtWorkflow = sourceAvailableBefore,
                DestinationDeficitKwhAtWorkflow = destinationNeedBefore,
                AmountKwh = amount,
                RemainingSourceSurplusKwhAfterWorkflow = null,
                RemainingDestinationDeficitKwhAfterWorkflow = null,
                BalanceDayUtc = dayStartUtc,
                TransferStatusEnum = TransferStatus.Planned,
                TriggerTypeEnum = TriggerType.Manual,
                AppliedDistributionModeEnum = TransferDistributionMode.Fair
            });

            runningRemainingSource = remainingSourceAfter;
        }

        return await UpsertManualPlannedRowsForSourceAsync(
            request.SourceAddressId,
            dayStartUtc,
            dayEndUtc,
            desired,
            ct);
    }

    private sealed record DestinationEntry(DestinationTransferRule Rule, AddressPosition Position);

    private static List<TransferWorkflow> AllocateFair(
        AddressPosition source,
        List<DestinationEntry> destinations,
        DateTime dayUtc)
    {
        var workflows = new List<TransferWorkflow>();

        var totalSurplus = source.RemainingSurplusKwh;
        var count = destinations.Count;

        if (count <= 0 || totalSurplus <= 0)
            return workflows;

        var share = decimal.Round(totalSurplus / count, 4);
        var runningRemainingSource = decimal.Round(totalSurplus, 4);

        foreach (var dest in destinations)
        {
            var sourceAvailableBefore = runningRemainingSource;
            var destinationNeedBefore = decimal.Round(dest.Position.RemainingDeficitKwh, 4);

            var amount = Math.Min(share, destinationNeedBefore);
            if (dest.Rule.MaxDailyKwh.HasValue)
                amount = Math.Min(amount, dest.Rule.MaxDailyKwh.Value);

            amount = decimal.Round(amount, 4);

            if (amount <= 0)
                continue;

            var remainingSourceAfter = decimal.Round(sourceAvailableBefore - amount, 4);

            workflows.Add(CreateWorkflow(
                source.AddressId,
                dest.Rule.DestinationAddressId,
                amount,
                sourceAvailableBefore,
                destinationNeedBefore,
                remainingSourceAfter,
                dayUtc,
                TransferDistributionMode.Fair,
                dest.Rule.Id,
                dest.Rule.Priority,
                dest.Rule.WeightPercent));

            runningRemainingSource = remainingSourceAfter;
        }

        return workflows;
    }

    private static List<TransferWorkflow> AllocatePriority(
        AddressPosition source,
        List<DestinationEntry> destinations,
        DateTime dayUtc)
    {
        var workflows = new List<TransferWorkflow>();
        var runningRemainingSource = decimal.Round(source.RemainingSurplusKwh, 4);

        foreach (var dest in destinations.OrderBy(x => x.Rule.Priority))
        {
            if (runningRemainingSource <= 0)
                break;

            var sourceAvailableBefore = runningRemainingSource;
            var destinationNeedBefore = decimal.Round(dest.Position.RemainingDeficitKwh, 4);

            var amount = Math.Min(sourceAvailableBefore, destinationNeedBefore);
            if (dest.Rule.MaxDailyKwh.HasValue)
                amount = Math.Min(amount, dest.Rule.MaxDailyKwh.Value);

            amount = decimal.Round(amount, 4);

            if (amount <= 0)
                continue;

            var remainingSourceAfter = decimal.Round(sourceAvailableBefore - amount, 4);

            workflows.Add(CreateWorkflow(
                source.AddressId,
                dest.Rule.DestinationAddressId,
                amount,
                sourceAvailableBefore,
                destinationNeedBefore,
                remainingSourceAfter,
                dayUtc,
                TransferDistributionMode.Priority,
                dest.Rule.Id,
                dest.Rule.Priority,
                dest.Rule.WeightPercent));

            runningRemainingSource = remainingSourceAfter;
        }

        return workflows;
    }

    private static List<TransferWorkflow> AllocateWeighted(
        AddressPosition source,
        List<DestinationEntry> destinations,
        DateTime dayUtc)
    {
        var workflows = new List<TransferWorkflow>();

        var totalSurplus = source.RemainingSurplusKwh;
        var totalWeight = destinations.Sum(x => x.Rule.WeightPercent ?? 0m);

        if (totalWeight <= 0 || totalSurplus <= 0)
            return workflows;

        var runningRemainingSource = decimal.Round(totalSurplus, 4);

        foreach (var dest in destinations)
        {
            if (runningRemainingSource <= 0)
                break;

            var sourceAvailableBefore = runningRemainingSource;
            var destinationNeedBefore = decimal.Round(dest.Position.RemainingDeficitKwh, 4);

            var weight = dest.Rule.WeightPercent ?? 0m;
            var targetAmount = decimal.Round(totalSurplus * (weight / totalWeight), 4);
            var amount = Math.Min(targetAmount, destinationNeedBefore);

            if (dest.Rule.MaxDailyKwh.HasValue)
                amount = Math.Min(amount, dest.Rule.MaxDailyKwh.Value);

            amount = decimal.Round(amount, 4);

            if (amount <= 0)
                continue;

            var remainingSourceAfter = decimal.Round(sourceAvailableBefore - amount, 4);

            workflows.Add(CreateWorkflow(
                source.AddressId,
                dest.Rule.DestinationAddressId,
                amount,
                sourceAvailableBefore,
                destinationNeedBefore,
                remainingSourceAfter,
                dayUtc,
                TransferDistributionMode.Weighted,
                dest.Rule.Id,
                dest.Rule.Priority,
                dest.Rule.WeightPercent));

            runningRemainingSource = remainingSourceAfter;
        }

        return workflows;
    }

    private static TransferWorkflow CreateWorkflow(
        int sourceId,
        int destId,
        decimal amount,
        decimal sourceSurplusAtWorkflow,
        decimal destinationDeficitAtWorkflow,
        decimal _remainingSourceSurplusAfterWorkflow,
        DateTime dayUtc,
        TransferDistributionMode distributionMode,
        int? destinationTransferRuleId,
        int? priority,
        decimal? weightPercent)
    {
        return new TransferWorkflow
        {
            SourceAddressId = sourceId,
            DestinationAddressId = destId,
            SourceSurplusKwhAtWorkflow = sourceSurplusAtWorkflow,
            DestinationDeficitKwhAtWorkflow = destinationDeficitAtWorkflow,
            AmountKwh = amount,
            RemainingSourceSurplusKwhAfterWorkflow = null,
            RemainingDestinationDeficitKwhAfterWorkflow = null,
            BalanceDayUtc = dayUtc,
            TransferStatusEnum = TransferStatus.Planned,
            TriggerTypeEnum = TriggerType.Auto,
            AppliedDistributionModeEnum = distributionMode,
            DestinationTransferRuleId = destinationTransferRuleId,
            Priority = priority,
            WeightPercent = weightPercent
        };
    }

    private async Task<List<TransferWorkflow>> UpsertAutoPlannedRowsForSourceAsync(
        int sourceAddressId,
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        List<TransferWorkflow> desiredWorkflows,
        CancellationToken ct)
    {
        return await UpsertPlannedRowsForSourceAsync(
            sourceAddressId,
            dayStartUtc,
            dayEndUtc,
            (int)TriggerType.Auto,
            desiredWorkflows,
            ct);
    }

    private async Task<List<TransferWorkflow>> UpsertManualPlannedRowsForSourceAsync(
        int sourceAddressId,
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        List<TransferWorkflow> desiredWorkflows,
        CancellationToken ct)
    {
        return await UpsertPlannedRowsForSourceAsync(
            sourceAddressId,
            dayStartUtc,
            dayEndUtc,
            (int)TriggerType.Manual,
            desiredWorkflows,
            ct);
    }

    private async Task<List<TransferWorkflow>> UpsertPlannedRowsForSourceAsync(
        int sourceAddressId,
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        int triggerType,
        List<TransferWorkflow> desiredWorkflows,
        CancellationToken ct)
    {
        var existingPlanned = await _db.TransferWorkflows
            .Where(x =>
                x.SourceAddressId == sourceAddressId &&
                x.BalanceDayUtc >= dayStartUtc &&
                x.BalanceDayUtc < dayEndUtc &&
                x.TriggerType == triggerType &&
                x.Status == (int)TransferStatus.Planned)
            .ToListAsync(ct);

        var existingByDestination = existingPlanned
            .GroupBy(x => x.DestinationAddressId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(w => w.Id).First());

        // If old duplicates already exist, keep the newest row per destination and delete the rest.
        var duplicatePlannedRows = existingPlanned
            .GroupBy(x => x.DestinationAddressId)
            .SelectMany(g => g.OrderByDescending(w => w.Id).Skip(1))
            .ToList();

        if (duplicatePlannedRows.Count > 0)
        {
            _logger.LogWarning(
                "Deleting {Count} duplicate Planned workflows for Source={Source}, TriggerType={TriggerType}, DayStart={DayStartUtc}",
                duplicatePlannedRows.Count,
                sourceAddressId,
                triggerType,
                dayStartUtc);

            _db.TransferWorkflows.RemoveRange(duplicatePlannedRows);
        }

        var desiredDestinationIds = desiredWorkflows
            .Select(x => x.DestinationAddressId)
            .ToHashSet();

        var obsoleteRows = existingByDestination.Values
            .Where(x => !desiredDestinationIds.Contains(x.DestinationAddressId))
            .ToList();

        if (obsoleteRows.Count > 0)
        {
            _logger.LogInformation(
                "Deleting {Count} obsolete Planned workflows for Source={Source}, TriggerType={TriggerType}, DayStart={DayStartUtc}",
                obsoleteRows.Count,
                sourceAddressId,
                triggerType,
                dayStartUtc);

            _db.TransferWorkflows.RemoveRange(obsoleteRows);
        }

        var result = new List<TransferWorkflow>();

        foreach (var desired in desiredWorkflows)
        {
            if (existingByDestination.TryGetValue(desired.DestinationAddressId, out var existing))
            {
                existing.SourceSurplusKwhAtWorkflow = desired.SourceSurplusKwhAtWorkflow;
                existing.DestinationDeficitKwhAtWorkflow = desired.DestinationDeficitKwhAtWorkflow;
                existing.AmountKwh = desired.AmountKwh;
                existing.RemainingSourceSurplusKwhAfterWorkflow = desired.RemainingSourceSurplusKwhAfterWorkflow;
                existing.RemainingDestinationDeficitKwhAfterWorkflow = desired.RemainingDestinationDeficitKwhAfterWorkflow;
                existing.AppliedDistributionMode = desired.AppliedDistributionMode;
                existing.DestinationTransferRuleId = desired.DestinationTransferRuleId;
                existing.Priority = desired.Priority;
                existing.WeightPercent = desired.WeightPercent;

                result.Add(existing);
            }
            else
            {
                _db.TransferWorkflows.Add(desired);
                result.Add(desired);
            }
        }

        if (duplicatePlannedRows.Count > 0 || obsoleteRows.Count > 0 || result.Count > 0)
            await _db.SaveChangesAsync(ct);

        return result;
    }

    private async Task<bool> HasNonPlannedAutoWorkflowsForSourceAsync(
        int sourceAddressId,
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        CancellationToken ct)
    {
        return await HasNonPlannedWorkflowsForSourceAsync(
            sourceAddressId,
            dayStartUtc,
            dayEndUtc,
            (int)TriggerType.Auto,
            ct);
    }

    private async Task<bool> HasNonPlannedManualWorkflowsForSourceAsync(
        int sourceAddressId,
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        CancellationToken ct)
    {
        return await HasNonPlannedWorkflowsForSourceAsync(
            sourceAddressId,
            dayStartUtc,
            dayEndUtc,
            (int)TriggerType.Manual,
            ct);
    }

    private async Task<bool> HasNonPlannedWorkflowsForSourceAsync(
        int sourceAddressId,
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        int triggerType,
        CancellationToken ct)
    {
        return await _db.TransferWorkflows.AnyAsync(x =>
            x.SourceAddressId == sourceAddressId &&
            x.BalanceDayUtc >= dayStartUtc &&
            x.BalanceDayUtc < dayEndUtc &&
            x.TriggerType == triggerType &&
            x.Status != (int)TransferStatus.Planned,
            ct);
    }

    private static DateTime ToUtcStartOfDay(DateOnly day)
    {
        return day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }

    private async Task<List<AddressPosition>> GetAddressPositionsAsync(
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        CancellationToken ct)
    {
        return await _db.DailyEnergyBalances
            .Where(x => x.Day >= dayStartUtc && x.Day < dayEndUtc)
            .Select(x => new AddressPosition
            {
                AddressId = x.AddressId,
                RemainingSurplusKwh = x.SurplusKwh,
                RemainingDeficitKwh = x.DeficitKwh
            })
            .ToListAsync(ct);
    }

    private async Task DeleteExistingAutoPlannedRowsForSourceAsync(
        int sourceAddressId,
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        CancellationToken ct)
    {
        var existing = await _db.TransferWorkflows
            .Where(x =>
                x.SourceAddressId == sourceAddressId &&
                x.BalanceDayUtc >= dayStartUtc &&
                x.BalanceDayUtc < dayEndUtc &&
                x.TriggerType == (int)TriggerType.Auto &&
                x.Status == (int)TransferStatus.Planned)
            .ToListAsync(ct);

        if (existing.Count > 0)
        {
            _logger.LogInformation(
                "Deleting {Count} auto Planned workflows for Source={Source}, DayStart={DayStartUtc}",
                existing.Count,
                sourceAddressId,
                dayStartUtc);

            _db.TransferWorkflows.RemoveRange(existing);
            await _db.SaveChangesAsync(ct);
        }
    }
}

public class AddressPosition
{
    public int AddressId { get; set; }
    public decimal RemainingSurplusKwh { get; set; }
    public decimal RemainingDeficitKwh { get; set; }
}


/* Best practical flow

Planned --Approve--> Approved --Execute--> Executed --Settle--> Settled
Planned --Reject--> Rejected
Approved --Cancel--> Cancelled
Approved --Execute fails--> Failed
Failed --Retry Execute--> Executed */
