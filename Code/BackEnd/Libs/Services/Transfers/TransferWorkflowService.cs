using Infrastructure.Enums;
using Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Models;

namespace EnergyManagement.Services.Transfers;

public class TransferWorkflowService : ITransferWorkflowService
{
    private const decimal Epsilon = 0.0001m;

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

        await DeleteExistingAutoPlannedRowsAsync(dayStartUtc, dayEndUtc, ct);

        var positions = await GetAddressPositionsAsync(dayStartUtc, dayEndUtc, ct);
        var positionsByAddress = positions.ToDictionary(x => x.AddressId);

        var rules = await _db.DestinationTransferRules
            .AsNoTracking()
            .Include(x => x.SourceTransferPolicy)
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.SourceTransferPolicy.SourceAddressId)
            .ThenBy(x => x.Priority)
            .ToListAsync(ct);

        var allWorkflows = new List<TransferWorkflow>();

        foreach (var sourceGroup in rules.GroupBy(x => x.SourceTransferPolicy.SourceAddressId))
        {
            if (!positionsByAddress.TryGetValue(sourceGroup.Key, out var sourcePosition))
            {
                _logger.LogInformation("No balance position found for source address {SourceAddressId}", sourceGroup.Key);
                continue;
            }

            if (sourcePosition.RemainingSurplusKwh <= Epsilon)
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

            var workflows = mode switch
            {
                TransferDistributionMode.Fair => AllocateFair(sourcePosition, sourceRules, positionsByAddress),
                TransferDistributionMode.Priority => AllocatePriority(sourcePosition, sourceRules, positionsByAddress),
                TransferDistributionMode.Weighted => AllocateWeighted(sourcePosition, sourceRules, positionsByAddress),
                _ => AllocateFair(sourcePosition, sourceRules, positionsByAddress)
            };

            allWorkflows.AddRange(workflows);
        }

        var created = new List<TransferWorkflow>();

        foreach (var candidate in allWorkflows)
        {
            var workflow = CreateTransferWorkflow(
                candidate,
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

        await DeleteExistingAutoPlannedRowsForSourceAsync(sourceAddressId, dayStartUtc, dayEndUtc, ct);

        var positions = await GetAddressPositionsAsync(dayStartUtc, dayEndUtc, ct);
        var positionsByAddress = positions.ToDictionary(x => x.AddressId);

        if (!positionsByAddress.TryGetValue(sourceAddressId, out var sourcePosition))
            return Array.Empty<TransferWorkflow>();

        if (sourcePosition.RemainingSurplusKwh <= Epsilon)
            return Array.Empty<TransferWorkflow>();

        var rules = await _db.DestinationTransferRules
            .AsNoTracking()
            .Include(x => x.SourceTransferPolicy)
            .Where(x => x.IsEnabled && x.SourceTransferPolicy.SourceAddressId == sourceAddressId)
            .OrderBy(x => x.Priority)
            .ToListAsync(ct);

        var mode = ResolveModeForSource(rules);

        var workflows = mode switch
        {
            TransferDistributionMode.Fair => AllocateFair(sourcePosition, rules, positionsByAddress),
            TransferDistributionMode.Priority => AllocatePriority(sourcePosition, rules, positionsByAddress),
            TransferDistributionMode.Weighted => AllocateWeighted(sourcePosition, rules, positionsByAddress),
            _ => AllocateFair(sourcePosition, rules, positionsByAddress)
        };

        var created = new List<TransferWorkflow>();

        foreach (var candidate in workflows)
        {
            var workflow = CreateTransferWorkflow(
                candidate,
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
            if (sourcePosition.RemainingSurplusKwh <= Epsilon)
                break;

            if (!positionsByAddress.TryGetValue(target.DestinationAddressId, out var destinationPosition))
                throw new InvalidOperationException(
                    $"Destination address {target.DestinationAddressId} has no balance position for {request.Day}.");

            if (target.DestinationAddressId == request.SourceAddressId)
                throw new InvalidOperationException("Source and destination cannot be the same address.");

            if (target.RequestedKwh <= 0)
                continue;

            var sourceAvailableBefore = decimal.Round(sourcePosition.RemainingSurplusKwh, 4);
            var destinationNeededBefore = decimal.Round(destinationPosition.RemainingDeficitKwh, 4);

            var transferKwh = Math.Min(target.RequestedKwh, sourceAvailableBefore);
            transferKwh = Math.Min(transferKwh, destinationNeededBefore);
            transferKwh = decimal.Round(transferKwh, 4);

            if (transferKwh <= Epsilon)
                continue;

            var candidate = new TransferWorkflow
            {
                SourceAddressId = request.SourceAddressId,
                DestinationAddressId = target.DestinationAddressId,
                SourceSurplusKwhAtWorkflow = sourceAvailableBefore,
                DestinationDeficitKwhAtWorkflow = destinationNeededBefore,
                AmountKwh = transferKwh,
                RemainingSourceSurplusKwhAfterWorkflow = decimal.Round(sourceAvailableBefore - transferKwh, 4),
                DestinationTransferRuleId = null,
                Priority = null,
                WeightPercent = null,
                AppliedDistributionModeEnum = TransferDistributionMode.Fair
            };

            var workflow = CreateTransferWorkflow(candidate, dayStartUtc, TriggerType.Manual, request.Notes);

            _db.TransferWorkflows.Add(workflow);
            created.Add(workflow);

            sourcePosition.AlreadyTransferredOutKwh += transferKwh;
            destinationPosition.AlreadyTransferredInKwh += transferKwh;

            _logger.LogInformation(
                "Manual planned transfer created: {Source} -> {Destination}, transfer={Amount}",
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
    List<DestinationTransferRule> sourceRules,
    Dictionary<int, AddressTransferPosition> positionsByAddress)
{
    var sourceAvailableAtStart = decimal.Round(sourcePosition.RemainingSurplusKwh, 4);
    var remainingSource = sourceAvailableAtStart;

    var allocationsByRuleId = new Dictionary<int, decimal>();
    var destinationNeedsAtStart = new Dictionary<int, decimal>();

    // Build initial candidates and capture starting needs ONCE
    var candidates = sourceRules
        .Where(rule => positionsByAddress.TryGetValue(rule.DestinationAddressId, out _))
        .Select(rule =>
        {
            var destination = positionsByAddress[rule.DestinationAddressId];

            var destinationNeedAtStart = decimal.Round(destination.RemainingDeficitKwh, 4);
            var maxForRule = rule.MaxDailyKwh ?? decimal.MaxValue;

            var capacity = decimal.Round(Math.Min(destinationNeedAtStart, maxForRule), 4);

            if (!destinationNeedsAtStart.ContainsKey(rule.DestinationAddressId))
                destinationNeedsAtStart[rule.DestinationAddressId] = destinationNeedAtStart;

            return new FairCandidate
            {
                Rule = rule,
                RemainingCapacity = capacity
            };
        })
        .Where(x => x.RemainingCapacity > Epsilon)
        .ToList();

    // Progressive equal-share distribution
    while (remainingSource > Epsilon)
    {
        var active = candidates
            .Where(x => x.RemainingCapacity > Epsilon)
            .ToList();

        if (active.Count == 0)
            break;

        var equalShare = decimal.Round(remainingSource / active.Count, 4);
        if (equalShare <= Epsilon)
            break;

        var allocatedThisRound = 0m;

        foreach (var candidate in active)
        {
            var transferKwh = decimal.Round(
                Math.Min(equalShare, candidate.RemainingCapacity), 4);

            if (transferKwh <= Epsilon)
                continue;

            allocationsByRuleId[candidate.Rule.Id] =
                allocationsByRuleId.TryGetValue(candidate.Rule.Id, out var current)
                    ? decimal.Round(current + transferKwh, 4)
                    : transferKwh;

            candidate.RemainingCapacity =
                decimal.Round(candidate.RemainingCapacity - transferKwh, 4);

            remainingSource =
                decimal.Round(remainingSource - transferKwh, 4);

            allocatedThisRound += transferKwh;
        }

        if (allocatedThisRound <= Epsilon)
            break;
    }

    // Build workflow rows using snapshots
    var result = BuildWorkflowRowsFromAllocations(
        sourceRules,
        positionsByAddress,
        allocationsByRuleId,
        destinationNeedsAtStart,
        sourceAvailableAtStart,
        TransferDistributionMode.Fair);

    // Only now mutate positions
    foreach (var workflow in result)
    {
        sourcePosition.AlreadyTransferredOutKwh += workflow.AmountKwh;

        if (positionsByAddress.TryGetValue(workflow.DestinationAddressId, out var dest))
            dest.AlreadyTransferredInKwh += workflow.AmountKwh;
    }

    return result;
}

    private List<TransferWorkflow> AllocatePriority(
        AddressTransferPosition sourcePosition,
        List<DestinationTransferRule> sourceRules,
        Dictionary<int, AddressTransferPosition> positionsByAddress)
    {
        var result = new List<TransferWorkflow>();
        var remainingSource = decimal.Round(sourcePosition.RemainingSurplusKwh, 4);

        foreach (var rule in sourceRules.OrderBy(x => x.Priority))
        {
            if (remainingSource <= Epsilon)
                break;

            if (!positionsByAddress.TryGetValue(rule.DestinationAddressId, out var destination))
                continue;

            var destinationNeedBefore = decimal.Round(destination.RemainingDeficitKwh, 4);
            if (destinationNeedBefore <= Epsilon)
                continue;

            var sourceAvailableBefore = remainingSource;
            var transferKwh = Math.Min(sourceAvailableBefore, destinationNeedBefore);

            if (rule.MaxDailyKwh.HasValue)
                transferKwh = Math.Min(transferKwh, rule.MaxDailyKwh.Value);

            transferKwh = decimal.Round(transferKwh, 4);
            if (transferKwh <= Epsilon)
                continue;

            remainingSource = decimal.Round(remainingSource - transferKwh, 4);
            sourcePosition.AlreadyTransferredOutKwh += transferKwh;
            destination.AlreadyTransferredInKwh += transferKwh;

                result.Add(new TransferWorkflow
            {
                SourceAddressId = rule.SourceTransferPolicy.SourceAddressId,
                DestinationAddressId = rule.DestinationAddressId,
                SourceSurplusKwhAtWorkflow = sourceAvailableBefore,
                DestinationDeficitKwhAtWorkflow = destinationNeedBefore,
                AmountKwh = transferKwh,
                RemainingSourceSurplusKwhAfterWorkflow = remainingSource,
                AppliedDistributionModeEnum = TransferDistributionMode.Priority,
                DestinationTransferRuleId = rule.Id,
                Priority = rule.Priority,
                WeightPercent = rule.WeightPercent
            });
        }

        return result;
    }

    private List<TransferWorkflow> AllocateWeighted(
        AddressTransferPosition sourcePosition,
        List<DestinationTransferRule> sourceRules,
        Dictionary<int, AddressTransferPosition> positionsByAddress)
    {
        var result = new List<TransferWorkflow>();
        var remainingSource = decimal.Round(sourcePosition.RemainingSurplusKwh, 4);

        while (remainingSource > Epsilon)
        {
            var eligible = sourceRules
                .Where(rule =>
                    rule.WeightPercent.HasValue &&
                    rule.WeightPercent.Value > 0 &&
                    positionsByAddress.TryGetValue(rule.DestinationAddressId, out var dest) &&
                    dest.RemainingDeficitKwh > Epsilon)
                .ToList();

            if (eligible.Count == 0)
                break;

            var totalWeight = eligible.Sum(x => x.WeightPercent!.Value);
            if (totalWeight <= Epsilon)
                break;

            var allocatedThisRound = 0m;

            foreach (var rule in eligible)
            {
                var destination = positionsByAddress[rule.DestinationAddressId];
                var sourceAvailableBefore = remainingSource;
                var destinationNeedBefore = decimal.Round(destination.RemainingDeficitKwh, 4);

                var ratio = rule.WeightPercent!.Value / totalWeight;
                var targetAmount = decimal.Round(sourceAvailableBefore * ratio, 4);

                var transferKwh = Math.Min(targetAmount, destinationNeedBefore);

                if (rule.MaxDailyKwh.HasValue)
                    transferKwh = Math.Min(transferKwh, rule.MaxDailyKwh.Value);

                transferKwh = decimal.Round(transferKwh, 4);
                if (transferKwh <= Epsilon)
                    continue;

                remainingSource = decimal.Round(remainingSource - transferKwh, 4);
                sourcePosition.AlreadyTransferredOutKwh += transferKwh;
                destination.AlreadyTransferredInKwh += transferKwh;
                allocatedThisRound += transferKwh;

                result.Add(new TransferWorkflow
                {
                    SourceAddressId = rule.SourceTransferPolicy.SourceAddressId,
                    DestinationAddressId = rule.DestinationAddressId,
                    SourceSurplusKwhAtWorkflow = sourceAvailableBefore,
                    DestinationDeficitKwhAtWorkflow = destinationNeedBefore,
                    AmountKwh = transferKwh,
                    RemainingSourceSurplusKwhAfterWorkflow = remainingSource,
                    AppliedDistributionModeEnum = TransferDistributionMode.Weighted,
                    DestinationTransferRuleId = rule.Id,
                    Priority = rule.Priority,
                    WeightPercent = rule.WeightPercent
                });
            }

            if (allocatedThisRound <= Epsilon)
                break;
        }

        return result;
    }

  private List<TransferWorkflow> BuildWorkflowRowsFromAllocations(
    List<DestinationTransferRule> sourceRules,
    Dictionary<int, AddressTransferPosition> positionsByAddress,
    Dictionary<int, decimal> allocationsByRuleId,
    Dictionary<int, decimal> destinationNeedsAtStart,
    decimal sourceAvailableAtStart,
    TransferDistributionMode mode)
{
    var result = new List<TransferWorkflow>();
    var runningRemainingSource = sourceAvailableAtStart;

    foreach (var rule in sourceRules.OrderBy(x => x.Priority))
    {
        if (!allocationsByRuleId.TryGetValue(rule.Id, out var transferKwh))
            continue;

        transferKwh = decimal.Round(transferKwh, 4);
        if (transferKwh <= Epsilon)
            continue;

        var sourceAvailableBefore = runningRemainingSource;
        runningRemainingSource =
            decimal.Round(runningRemainingSource - transferKwh, 4);

        result.Add(new TransferWorkflow
        {
            SourceAddressId = rule.SourceTransferPolicy.SourceAddressId,
            DestinationAddressId = rule.DestinationAddressId,

            SourceSurplusKwhAtWorkflow = sourceAvailableBefore,
            DestinationDeficitKwhAtWorkflow =
                destinationNeedsAtStart[rule.DestinationAddressId],

            AmountKwh = transferKwh,
            RemainingSourceSurplusKwhAfterWorkflow = runningRemainingSource,

            AppliedDistributionModeEnum = mode,
            DestinationTransferRuleId = rule.Id,
            Priority = rule.Priority,
            WeightPercent = rule.WeightPercent
        });
    }

    return result;
}

    private TransferDistributionMode ResolveModeForSource(List<DestinationTransferRule> sourceRules)
    {
        var modeFromRule = sourceRules
            .Select(x => x.DistributionMode)
            .Distinct()
            .ToList();

        if (modeFromRule.Count > 1)
        {
            throw new InvalidOperationException(
                $"Source address {sourceRules.First().SourceTransferPolicy.SourceAddressId} has conflicting distribution modes.");
        }

        if (modeFromRule.Count == 1)
            return (TransferDistributionMode)modeFromRule[0];

        return _optionsMonitor.CurrentValue.DistributionMode;
    }

    private static DateTime ToUtcStartOfDay(DateOnly day) =>
        DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static TransferWorkflow CreateTransferWorkflow(
        TransferWorkflow workflow,
        DateTime balanceDayUtc,
        TriggerType triggerType,
        string? notes)
    {
        return new TransferWorkflow
        {
            BalanceDayUtc = balanceDayUtc,
            EffectiveAtUtc = DateTime.UtcNow,
            SourceAddressId = workflow.SourceAddressId,
            DestinationAddressId = workflow.DestinationAddressId,
            SourceSurplusKwhAtWorkflow = workflow.SourceSurplusKwhAtWorkflow,
            DestinationDeficitKwhAtWorkflow = workflow.DestinationDeficitKwhAtWorkflow,
            AmountKwh = workflow.AmountKwh,
            RemainingSourceSurplusKwhAfterWorkflow = workflow.RemainingSourceSurplusKwhAfterWorkflow,
            Priority = workflow.Priority,
            WeightPercent = workflow.WeightPercent,
            DestinationTransferRuleId = workflow.DestinationTransferRuleId,
            TriggerTypeEnum = triggerType,
            TransferStatusEnum = TransferStatus.Planned,
            AppliedDistributionMode = workflow.AppliedDistributionMode,
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

 private sealed class FairCandidate
{
    public required DestinationTransferRule Rule { get; init; }
    public decimal RemainingCapacity { get; set; }
}
}
