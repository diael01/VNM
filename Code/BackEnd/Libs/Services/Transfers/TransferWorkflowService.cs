using Infrastructure.Enums;
using Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Models;

namespace EnergyManagement.Services.Transfers;

public class TransferWorkflowService : ITransferWorkflowService
{
    private readonly VnmDbContext _db;
    private readonly ILogger<TransferWorkflowService> _logger;
    private readonly IOptionsMonitor<TransferWorkflowOptions> _optionsMonitor;

    public TransferWorkflowService(
        VnmDbContext db,
        ILogger<TransferWorkflowService> logger,
        IOptionsMonitor<TransferWorkflowOptions> optionsMonitor)
    {
        _db = db;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    public async Task<IReadOnlyList<TransferWorkflow>> RunAutomaticWorkflowAsync(
        DateOnly day,
        CancellationToken ct = default)
    {
        var dayStartUtc = ToUtcStartOfDay(day);
        var dayEndUtc = dayStartUtc.AddDays(1);

        // Refresh auto-planned rows for this day so planner does not duplicate itself forever.
        await DeleteExistingAutoPlannedRowsAsync(dayStartUtc, dayEndUtc, ct);

        var positions = await GetAddressPositionsAsync(dayStartUtc, dayEndUtc, ct);
        var positionsByAddress = positions.ToDictionary(x => x.AddressId);

        var rules = await _db.TransferRules
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.SourceAddressId)
            .ThenBy(x => x.Priority)
            .ToListAsync(ct);

        var allWorkflows = new List<TransferWorkflow>();

        foreach (var sourceGroup in rules.GroupBy(x => x.SourceAddressId))
        {
            if (!positionsByAddress.TryGetValue(sourceGroup.Key, out var sourcePosition))
            {
                _logger.LogInformation("No balance position found for source address {SourceAddressId}", sourceGroup.Key);
                continue;
            }

            if (sourcePosition.RemainingSurplusKwh <= 0.0001m)
            {
                _logger.LogInformation(
                    "Source address {SourceAddressId} has no remaining surplus. RemainingSurplus={RemainingSurplus}",
                    sourceGroup.Key,
                    sourcePosition.RemainingSurplusKwh);
                continue;
            }

            var sourceRules = sourceGroup.ToList();
            var mode = ResolveModeForSource(sourceRules);

            _logger.LogInformation(
                "Planning for source address {SourceAddressId}. Mode={Mode}, RemainingSurplus={RemainingSurplus}, Rules={RuleCount}",
                sourceGroup.Key,
                mode,
                sourcePosition.RemainingSurplusKwh,
                sourceRules.Count);

            var Workflows = mode switch
            {
                TransferDistributionMode.Fair =>
                    AllocateFair(sourcePosition, sourceRules, positionsByAddress),

                TransferDistributionMode.Priority =>
                    AllocatePriority(sourcePosition, sourceRules, positionsByAddress),

                TransferDistributionMode.Weighted =>
                    AllocateWeighted(sourcePosition, sourceRules, positionsByAddress),

                _ => AllocateFair(sourcePosition, sourceRules, positionsByAddress)
            };

            allWorkflows.AddRange(Workflows);
        }

        var created = new List<TransferWorkflow>();

        foreach (var Workflow in allWorkflows)
        {
            var workflow = CreateTransferWorkflow(
                Workflow,
                dayStartUtc,
                TriggerType.Auto,
                "Automatic planned transfer");

            _db.TransferWorkflows.Add(workflow);
            created.Add(workflow);
        }

        if (created.Count > 0)
            await _db.SaveChangesAsync(ct);

        return created;
    }

    public async Task<IReadOnlyList<TransferWorkflow>> RunAutomaticWorkflowForSourceAsync(
        int sourceAddressId,
        DateOnly day,
        CancellationToken ct = default)
    {
        var dayStartUtc = ToUtcStartOfDay(day);
        var dayEndUtc = dayStartUtc.AddDays(1);

        // Refresh only this source's auto-planned rows for the day.
        await DeleteExistingAutoPlannedRowsForSourceAsync(sourceAddressId, dayStartUtc, dayEndUtc, ct);

        var positions = await GetAddressPositionsAsync(dayStartUtc, dayEndUtc, ct);
        var positionsByAddress = positions.ToDictionary(x => x.AddressId);

        if (!positionsByAddress.TryGetValue(sourceAddressId, out var sourcePosition))
            return Array.Empty<TransferWorkflow>();

        if (sourcePosition.RemainingSurplusKwh <= 0.0001m)
            return Array.Empty<TransferWorkflow>();

        var rules = await _db.TransferRules
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.SourceAddressId == sourceAddressId)
            .OrderBy(x => x.Priority)
            .ToListAsync(ct);

        var mode = ResolveModeForSource(rules);

        var Workflows = mode switch
        {
            TransferDistributionMode.Fair =>
                AllocateFair(sourcePosition, rules, positionsByAddress),

            TransferDistributionMode.Priority =>
                AllocatePriority(sourcePosition, rules, positionsByAddress),

            TransferDistributionMode.Weighted =>
                AllocateWeighted(sourcePosition, rules, positionsByAddress),

            _ => AllocateFair(sourcePosition, rules, positionsByAddress)
        };

        var created = new List<TransferWorkflow>();

        foreach (var Workflow in Workflows)
        {
            var workflow = CreateTransferWorkflow(
                Workflow,
                dayStartUtc,
                TriggerType.Auto,
                $"Automatic planned transfer for source {sourceAddressId}");

            _db.TransferWorkflows.Add(workflow);
            created.Add(workflow);
        }

        if (created.Count > 0)
            await _db.SaveChangesAsync(ct);

        return created;
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

        var created = new List<TransferWorkflow>();

        foreach (var target in request.Targets)
        {
            if (sourcePosition.RemainingSurplusKwh <= 0.0001m)
                break;

            if (!positionsByAddress.TryGetValue(target.DestinationAddressId, out var destinationPosition))
                throw new InvalidOperationException(
                    $"Destination address {target.DestinationAddressId} has no balance position for {request.Day}.");

            if (target.DestinationAddressId == request.SourceAddressId)
                throw new InvalidOperationException("Source and destination cannot be the same address.");

            if (target.RequestedKwh <= 0)
                continue;

            var amount = Math.Min(target.RequestedKwh, sourcePosition.RemainingSurplusKwh);
            amount = Math.Min(amount, destinationPosition.RemainingDeficitKwh);
            amount = decimal.Round(amount, 4);

            if (amount <= 0.0001m)
                continue;

            var Workflow = new TransferWorkflow
            {
                SourceAddressId = request.SourceAddressId,
                DestinationAddressId = target.DestinationAddressId,                
                AmountKwh = amount,
                TransferRuleId = null,
                AppliedDistributionModeEnum = TransferDistributionMode.Fair
            };

            var workflow = CreateTransferWorkflow(
                Workflow,
                dayStartUtc,
                TriggerType.Manual,
                request.Notes);

            _db.TransferWorkflows.Add(workflow);
            created.Add(workflow);

            // Prevent overbooking inside the SAME manual request.
            sourcePosition.AlreadyTransferredOutKwh += amount;
            destinationPosition.AlreadyTransferredInKwh += amount;

            _logger.LogInformation(
                "Manual planned transfer created: {Source} -> {Destination}, planned={Amount}",
                workflow.SourceAddressId,
                workflow.DestinationAddressId,                
                workflow.AmountKwh);
        }

        if (created.Count > 0)
            await _db.SaveChangesAsync(ct);

        return created;
    }

    private async Task<List<AddressTransferPosition>> GetAddressPositionsAsync(
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        CancellationToken ct)
    {
        var dailyBalances = await _db.DailyEnergyBalances
            .AsNoTracking()
            .Where(x => x.Day >= dayStartUtc && x.Day < dayEndUtc)
            .GroupBy(x => x.AddressId)
            .Select(g => new
            {
                AddressId = g.Key,
                DailySurplusKwh = g.Sum(x => x.SurplusKwh),
                DailyDeficitKwh = g.Sum(x => x.DeficitKwh)
            })
            .ToListAsync(ct);

        // Only REAL transfers reduce remaining.
        var effectiveStatuses = new[]
        {
            (int)TransferStatus.Executed,
            (int)TransferStatus.Settled
        };

        var transferredOut = await _db.TransferWorkflows
            .AsNoTracking()
            .Where(x => x.BalanceDayUtc >= dayStartUtc &&
                        x.BalanceDayUtc < dayEndUtc &&
                        effectiveStatuses.Contains(x.Status))
            .GroupBy(x => x.SourceAddressId)
            .Select(g => new
            {
                AddressId = g.Key,
                Amount = g.Sum(x => x.AmountKwh)
            })
            .ToListAsync(ct);

        var transferredIn = await _db.TransferWorkflows
            .AsNoTracking()
            .Where(x => x.BalanceDayUtc >= dayStartUtc &&
                        x.BalanceDayUtc < dayEndUtc &&
                        effectiveStatuses.Contains(x.Status))
            .GroupBy(x => x.DestinationAddressId)
            .Select(g => new
            {
                AddressId = g.Key,
                Amount = g.Sum(x => x.AmountKwh)
            })
            .ToListAsync(ct);

        var outDict = transferredOut.ToDictionary(x => x.AddressId, x => x.Amount);
        var inDict = transferredIn.ToDictionary(x => x.AddressId, x => x.Amount);

        return dailyBalances
            .Select(x => new AddressTransferPosition
            {
                AddressId = x.AddressId,
                DailySurplusKwh = x.DailySurplusKwh,
                DailyDeficitKwh = x.DailyDeficitKwh,
                AlreadyTransferredOutKwh = outDict.TryGetValue(x.AddressId, out var outAmount) ? outAmount : 0m,
                AlreadyTransferredInKwh = inDict.TryGetValue(x.AddressId, out var inAmount) ? inAmount : 0m
            })
            .ToList();
    }

    private List<TransferWorkflow> AllocateFair(
        AddressTransferPosition sourcePosition,
        List<TransferRule> sourceRules,
        Dictionary<int, AddressTransferPosition> positionsByAddress)
    {
        var result = new List<TransferWorkflow>();

        while (sourcePosition.RemainingSurplusKwh > 0.0001m)
        {
            var eligible = sourceRules
                .Where(rule =>
                    positionsByAddress.TryGetValue(rule.DestinationAddressId, out var dest) &&
                    dest.RemainingDeficitKwh > 0.0001m)
                .ToList();

            if (eligible.Count == 0)
                break;

            var equalShare = decimal.Round(sourcePosition.RemainingSurplusKwh / eligible.Count, 4);
            if (equalShare <= 0.0001m)
                break;

            var allocatedThisRound = 0m;

            foreach (var rule in eligible)
            {
                var destination = positionsByAddress[rule.DestinationAddressId];

                var amount = Math.Min(equalShare, destination.RemainingDeficitKwh);

                if (rule.MaxDailyKwh.HasValue)
                    amount = Math.Min(amount, rule.MaxDailyKwh.Value);

                amount = decimal.Round(amount, 4);

                if (amount <= 0.0001m)
                    continue;

                result.Add(new TransferWorkflow
                {
                    SourceAddressId = rule.SourceAddressId,
                    DestinationAddressId = rule.DestinationAddressId,
                    TransferRuleId = rule.Id,
                    AmountKwh = amount,
                    AppliedDistributionModeEnum = TransferDistributionMode.Fair
                });

                // Prevent overbooking within THIS planning run only.
                sourcePosition.AlreadyTransferredOutKwh += amount;
                destination.AlreadyTransferredInKwh += amount;
                allocatedThisRound += amount;
            }

            if (allocatedThisRound <= 0.0001m)
                break;
        }

        return MergeWorkflows(result);
    }

    private List<TransferWorkflow> AllocatePriority(
        AddressTransferPosition sourcePosition,
        List<TransferRule> sourceRules,
        Dictionary<int, AddressTransferPosition> positionsByAddress)
    {
        var result = new List<TransferWorkflow>();

        foreach (var rule in sourceRules.OrderBy(x => x.Priority))
        {
            if (sourcePosition.RemainingSurplusKwh <= 0.0001m)
                break;

            if (!positionsByAddress.TryGetValue(rule.DestinationAddressId, out var destination))
                continue;

            if (destination.RemainingDeficitKwh <= 0.0001m)
                continue;

            var amount = Math.Min(sourcePosition.RemainingSurplusKwh, destination.RemainingDeficitKwh);

            if (rule.MaxDailyKwh.HasValue)
                amount = Math.Min(amount, rule.MaxDailyKwh.Value);

            amount = decimal.Round(amount, 4);

            if (amount <= 0.0001m)
                continue;

            result.Add(new TransferWorkflow
            {
                SourceAddressId = rule.SourceAddressId,
                DestinationAddressId = rule.DestinationAddressId,
                AmountKwh = amount,
                AppliedDistributionModeEnum = TransferDistributionMode.Priority,
                TransferRuleId = rule.Id
            });

            sourcePosition.AlreadyTransferredOutKwh += amount;
            destination.AlreadyTransferredInKwh += amount;
        }

        return result;
    }

    private List<TransferWorkflow> AllocateWeighted(
        AddressTransferPosition sourcePosition,
        List<TransferRule> sourceRules,
        Dictionary<int, AddressTransferPosition> positionsByAddress)
    {
        var result = new List<TransferWorkflow>();

        while (sourcePosition.RemainingSurplusKwh > 0.0001m)
        {
            var eligible = sourceRules
                .Where(rule =>
                    rule.WeightPercent.HasValue &&
                    rule.WeightPercent.Value > 0 &&
                    positionsByAddress.TryGetValue(rule.DestinationAddressId, out var dest) &&
                    dest.RemainingDeficitKwh > 0.0001m)
                .ToList();

            if (eligible.Count == 0)
                break;

            var totalWeight = eligible.Sum(x => x.WeightPercent!.Value);
            if (totalWeight <= 0.0001m)
                break;

            var allocatedThisRound = 0m;

            foreach (var rule in eligible)
            {
                var destination = positionsByAddress[rule.DestinationAddressId];

                var ratio = rule.WeightPercent!.Value / totalWeight;
                var targetAmount = decimal.Round(sourcePosition.RemainingSurplusKwh * ratio, 4);

                var amount = Math.Min(targetAmount, destination.RemainingDeficitKwh);

                if (rule.MaxDailyKwh.HasValue)
                    amount = Math.Min(amount, rule.MaxDailyKwh.Value);

                amount = decimal.Round(amount, 4);

                if (amount <= 0.0001m)
                    continue;

                result.Add(new TransferWorkflow
                {
                    SourceAddressId = rule.SourceAddressId,
                    DestinationAddressId = rule.DestinationAddressId,
                    AmountKwh = amount,
                    AppliedDistributionModeEnum = TransferDistributionMode.Weighted,
                    TransferRuleId = rule.Id
                });

                sourcePosition.AlreadyTransferredOutKwh += amount;
                destination.AlreadyTransferredInKwh += amount;
                allocatedThisRound += amount;
            }

            if (allocatedThisRound <= 0.0001m)
                break;
        }

        return MergeWorkflows(result);
    }

    private static List<TransferWorkflow> MergeWorkflows(List<TransferWorkflow> Workflows)
    {
        return Workflows
            .GroupBy(x => new { x.SourceAddressId, x.DestinationAddressId })
            .Select(g => new TransferWorkflow
            {
                SourceAddressId = g.Key.SourceAddressId,
                DestinationAddressId = g.Key.DestinationAddressId,
                AmountKwh = decimal.Round(g.Sum(x => x.AmountKwh), 4),
                TransferRuleId = g.First().TransferRuleId,
                AppliedDistributionMode = g.First().AppliedDistributionMode
            })
            .ToList();
    }

    private TransferDistributionMode ResolveModeForSource(List<TransferRule> sourceRules)
    {
        var modeFromRule = sourceRules
            .Select(x => x.DistributionMode)
            .Distinct()
            .ToList();

        if (modeFromRule.Count > 1)
        {
            throw new InvalidOperationException(
                $"Source address {sourceRules.First().SourceAddressId} has conflicting distribution modes.");
        }

        if (modeFromRule.Count == 1)
            return (TransferDistributionMode)modeFromRule[0];

        return _optionsMonitor.CurrentValue.DistributionMode;
    }

    private static DateTime ToUtcStartOfDay(DateOnly day) =>
        DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static TransferWorkflow CreateTransferWorkflow(
        TransferWorkflow Workflow,
        DateTime balanceDayUtc,
        TriggerType triggerType,
        string? notes)
    {
        return new TransferWorkflow
        {
            BalanceDayUtc = balanceDayUtc,
            EffectiveAtUtc = DateTime.UtcNow,
            SourceAddressId = Workflow.SourceAddressId,
            DestinationAddressId = Workflow.DestinationAddressId,
            AmountKwh = Workflow.AmountKwh,
            TransferRuleId = Workflow.TransferRuleId,
            TriggerTypeEnum = triggerType,
            TransferStatusEnum = TransferStatus.Planned, // planner creates proposals, not executions
            AppliedDistributionMode = Workflow.AppliedDistributionMode,
            Notes = notes,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private async Task DeleteExistingAutoPlannedRowsAsync(
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        CancellationToken ct)
    {
        await _db.TransferWorkflows
            .Where(x => x.BalanceDayUtc >= dayStartUtc &&
                        x.BalanceDayUtc < dayEndUtc &&
                        x.TriggerType == (int)TriggerType.Auto &&
                        x.Status == (int)TransferStatus.Planned)
            .ExecuteDeleteAsync(ct);
    }

    private async Task DeleteExistingAutoPlannedRowsForSourceAsync(
        int sourceAddressId,
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        CancellationToken ct)
    {
        await _db.TransferWorkflows
            .Where(x => x.SourceAddressId == sourceAddressId &&
                        x.BalanceDayUtc >= dayStartUtc &&
                        x.BalanceDayUtc < dayEndUtc &&
                        x.TriggerType == (int)TriggerType.Auto &&
                        x.Status == (int)TransferStatus.Planned)
            .ExecuteDeleteAsync(ct);
    }

    Task<IReadOnlyList<TransferWorkflow>> ITransferWorkflowService.ExecuteManualTransferAsync(ManualTransferRequest request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}