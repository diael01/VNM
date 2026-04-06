using Azure.Core;
using Infrastructure.Enums;
using Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Models;

namespace EnergyManagement.Services.Transfers;

public class TransferAllocationService : ITransferAllocationService
{
private readonly VnmDbContext _db;
private readonly ILogger<TransferAllocationService> _logger;
private readonly IOptionsMonitor<TransferAllocationOptions> _optionsMonitor;

public TransferAllocationService(
    VnmDbContext db,
    ILogger<TransferAllocationService> logger,
    IOptionsMonitor<TransferAllocationOptions> optionsMonitor)
{
    _db = db;
    _logger = logger;
    _optionsMonitor = optionsMonitor;
}


    public async Task<IReadOnlyList<TransferExecution>> RunAutomaticAllocationAsync(
    DateOnly day,
    CancellationToken ct = default)
{
    var dayStartUtc = ToUtcStartOfDay(day);
    var dayEndUtc = dayStartUtc.AddDays(1);

    var positions = await GetAddressPositionsAsync(dayStartUtc, dayEndUtc, ct);
    var positionsByAddress = positions.ToDictionary(x => x.AddressId);

    var rules = await _db.TransferRules
        .AsNoTracking()
        .Where(x => x.IsEnabled)
        .OrderBy(x => x.SourceAddressId)
        .ThenBy(x => x.Priority)
        .ToListAsync(ct);

    var allAllocations = new List<TransferAllocation>();

    foreach (var sourceGroup in rules.GroupBy(x => x.SourceAddressId))
    {
        if (!positionsByAddress.TryGetValue(sourceGroup.Key, out var sourcePosition))
            continue;

        if (sourcePosition.RemainingSurplusKwh <= 0.0001m)
            continue;

        var sourceRules = sourceGroup.ToList();
        var mode = ResolveModeForSource(sourceRules);

        List<TransferAllocation> allocations = mode switch
        {
            TransferDistributionMode.Fair =>
                AllocateFair(sourcePosition, sourceRules, positionsByAddress),

            TransferDistributionMode.Priority =>
                AllocatePriority(sourcePosition, sourceRules, positionsByAddress),

            TransferDistributionMode.Weighted =>
                AllocateWeighted(sourcePosition, sourceRules, positionsByAddress),

            _ => AllocateFair(sourcePosition, sourceRules, positionsByAddress)
        };

        allAllocations.AddRange(allocations);
    }

    var created = new List<TransferExecution>();

    foreach (var allocation in allAllocations)
    {
        var transfer = new TransferExecution
        {
            BalanceDayUtc = dayStartUtc,
            EffectiveAtUtc = DateTime.UtcNow,
            SourceAddressId = allocation.SourceAddressId,
            DestinationAddressId = allocation.DestinationAddressId,
            RequestedKwh = allocation.RequestedKwh,
            AllocatedKwh = allocation.AllocatedKwh,
            TriggerTypeEnum = TriggerType.Auto,
            TransferStatusEnum = TransferStatus.Executed,
            Notes = "Automatic allocation",
            CreatedAtUtc = DateTime.UtcNow,
            TransferRuleId = allocation.TransferRuleId,
            AppliedDistributionModeEnum = allocation.AppliedDistributionMode,           
        };

        _db.TransferExecutions.Add(transfer);
        created.Add(transfer);
    }

    if (created.Count > 0)
        await _db.SaveChangesAsync(ct);

    return created;
}

/// <summary>
/// Allocate Fairly
/// </summary>
/// <param name="sourcePosition"></param>
/// <param name="sourceRules"></param>
/// <param name="positionsByAddress"></param>
/// <returns></returns>
private List<TransferAllocation> AllocateFair(
    AddressTransferPosition sourcePosition,
    List<TransferRule> sourceRules,
    Dictionary<int, AddressTransferPosition> positionsByAddress)
{
    var result = new List<TransferAllocation>();

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

            result.Add(new TransferAllocation
            {
                SourceAddressId = rule.SourceAddressId,
                DestinationAddressId = rule.DestinationAddressId,
                TransferRuleId = rule.Id,
                RequestedKwh = amount,
                AllocatedKwh = amount,
                AppliedDistributionMode = TransferDistributionMode.Fair,              
            });

            sourcePosition.AlreadyTransferredOutKwh += amount;
            destination.AlreadyTransferredInKwh += amount;
            allocatedThisRound += amount;
        }

        if (allocatedThisRound <= 0.0001m)
            break;
    }

    return MergeAllocations(result);
}

/// <summary>
/// Allocate priority
/// </summary>
/// <param name="sourcePosition"></param>
/// <param name="sourceRules"></param>
/// <param name="positionsByAddress"></param>
/// <returns></returns>
private List<TransferAllocation> AllocatePriority(
    AddressTransferPosition sourcePosition,
    List<TransferRule> sourceRules,
    Dictionary<int, AddressTransferPosition> positionsByAddress)
{
    var result = new List<TransferAllocation>();

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

        result.Add(new TransferAllocation
        {
            SourceAddressId = rule.SourceAddressId,
            DestinationAddressId = rule.DestinationAddressId,
            RequestedKwh = amount,
            AllocatedKwh = amount,
            AppliedDistributionMode = TransferDistributionMode.Priority,
            TransferRuleId = rule.Id
        });

        sourcePosition.AlreadyTransferredOutKwh += amount;
        destination.AlreadyTransferredInKwh += amount;
    }

    return result;
}

/// <summary>
/// Allocate weighted
/// </summary>
/// <param name="sourcePosition"></param>
/// <param name="sourceRules"></param>
/// <param name="positionsByAddress"></param>
/// <returns></returns>
private List<TransferAllocation> AllocateWeighted(
    AddressTransferPosition sourcePosition,
    List<TransferRule> sourceRules,
    Dictionary<int, AddressTransferPosition> positionsByAddress)
{
    var result = new List<TransferAllocation>();

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

            result.Add(new TransferAllocation
            {
                SourceAddressId = rule.SourceAddressId,
                DestinationAddressId = rule.DestinationAddressId,
                RequestedKwh = targetAmount,
                AllocatedKwh = amount,
                AppliedDistributionMode = TransferDistributionMode.Weighted,
                TransferRuleId = rule.Id
            });

            sourcePosition.AlreadyTransferredOutKwh += amount;
            destination.AlreadyTransferredInKwh += amount;
            allocatedThisRound += amount;
        }

        if (allocatedThisRound <= 0.0001m)
            break;
    }

    return MergeAllocations(result);
}

    public async Task<IReadOnlyList<TransferExecution>> RunAutomaticAllocationForSourceAsync(
        int sourceAddressId,
        DateOnly day,
        CancellationToken ct = default)
    {
        var dayStartUtc = ToUtcStartOfDay(day);
        var dayEndUtc = dayStartUtc.AddDays(1);

        var positions = await GetAddressPositionsAsync(dayStartUtc, dayEndUtc, ct);
        var positionsByAddress = positions.ToDictionary(x => x.AddressId);

        if (!positionsByAddress.TryGetValue(sourceAddressId, out var sourcePosition))
            return Array.Empty<TransferExecution>();

        if (sourcePosition.RemainingSurplusKwh <= 0.0001m)
            return Array.Empty<TransferExecution>();

        var rules = await _db.Set<TransferRule>()
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.SourceAddressId == sourceAddressId)
            .OrderBy(x => x.Priority)
            .ToListAsync(ct);

        var created = new List<TransferExecution>();

        foreach (var rule in rules)
        {
            if (sourcePosition.RemainingSurplusKwh <= 0.0001m)
                break;

            if (!positionsByAddress.TryGetValue(rule.DestinationAddressId, out var destinationPosition))
                continue;

            if (destinationPosition.RemainingDeficitKwh <= 0.0001m)
                continue;

            var amount = Math.Min(sourcePosition.RemainingSurplusKwh, destinationPosition.RemainingDeficitKwh);

            if (rule.MaxDailyKwh.HasValue)
                amount = Math.Min(amount, rule.MaxDailyKwh.Value);

            amount = decimal.Round(amount, 4);

            if (amount <= 0.0001m)
                continue;

            var transfer = new TransferExecution
            {
                BalanceDayUtc = dayStartUtc,
                EffectiveAtUtc = DateTime.UtcNow,
                SourceAddressId = rule.SourceAddressId,
                DestinationAddressId = rule.DestinationAddressId,
                TransferRuleId = rule.Id,
                RequestedKwh = amount,
                AllocatedKwh = amount,
                TriggerTypeEnum = TriggerType.Auto,
                TransferStatusEnum = TransferStatus.Executed,
                Notes = $"Auto allocation for source {sourceAddressId}",
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Set<TransferExecution>().Add(transfer);
            created.Add(transfer);

            sourcePosition.AlreadyTransferredOutKwh += amount;
            destinationPosition.AlreadyTransferredInKwh += amount;
        }

        if (created.Count > 0)
            await _db.SaveChangesAsync(ct);

        return created;
    }

    public async Task<IReadOnlyList<TransferExecution>> ExecuteManualTransferAsync(
        ManualTransferRequest request,
        CancellationToken ct = default)
    {
        if (request.Targets == null || request.Targets.Count == 0)
            return Array.Empty<TransferExecution>();

        var dayStartUtc = ToUtcStartOfDay(request.Day);
        var dayEndUtc = dayStartUtc.AddDays(1);

        var positions = await GetAddressPositionsAsync(dayStartUtc, dayEndUtc, ct);
        var positionsByAddress = positions.ToDictionary(x => x.AddressId);

        if (!positionsByAddress.TryGetValue(request.SourceAddressId, out var sourcePosition))
            throw new InvalidOperationException($"Source address {request.SourceAddressId} has no balance position for {request.Day}.");

        var created = new List<TransferExecution>();

        foreach (var target in request.Targets)
        {
            if (sourcePosition.RemainingSurplusKwh <= 0.0001m)
                break;

            if (!positionsByAddress.TryGetValue(target.DestinationAddressId, out var destinationPosition))
                throw new InvalidOperationException($"Destination address {target.DestinationAddressId} has no balance position for {request.Day}.");

            if (target.DestinationAddressId == request.SourceAddressId)
                throw new InvalidOperationException("Source and destination cannot be the same address.");

            if (target.RequestedKwh <= 0)
                continue;

            var amount = Math.Min(target.RequestedKwh, sourcePosition.RemainingSurplusKwh);
            amount = Math.Min(amount, destinationPosition.RemainingDeficitKwh);
            amount = decimal.Round(amount, 4);

            if (amount <= 0.0001m)
                continue;

            var transfer = new TransferExecution
            {
                BalanceDayUtc = dayStartUtc,
                EffectiveAtUtc = DateTime.UtcNow,
                SourceAddressId = request.SourceAddressId,
                DestinationAddressId = target.DestinationAddressId,
                RequestedKwh = decimal.Round(target.RequestedKwh, 4),
                AllocatedKwh = amount,
                TriggerTypeEnum = TriggerType.Manual,
                TransferStatusEnum = TransferStatus.Executed,
                Notes = request.Notes,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Set<TransferExecution>().Add(transfer);
            created.Add(transfer);

            sourcePosition.AlreadyTransferredOutKwh += amount;
            destinationPosition.AlreadyTransferredInKwh += amount;

            _logger.LogInformation(
                "Manual transfer created: {Source} -> {Destination}, requested={Requested}, allocated={Allocated}",
                transfer.SourceAddressId,
                transfer.DestinationAddressId,
                transfer.RequestedKwh,
                transfer.AllocatedKwh);
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

        var transferredOut = await _db.Set<TransferExecution>()
            .AsNoTracking()
            .Where(x => x.BalanceDayUtc >= dayStartUtc && x.BalanceDayUtc < dayEndUtc
                     && x.Status == (int)TransferStatus.Executed)
            .GroupBy(x => x.SourceAddressId)
            .Select(g => new
            {
                AddressId = g.Key,
                Amount = g.Sum(x => x.AllocatedKwh)
            })
            .ToListAsync(ct);

        var transferredIn = await _db.Set<TransferExecution>()
            .AsNoTracking()
            .Where(x => x.BalanceDayUtc >= dayStartUtc && x.BalanceDayUtc < dayEndUtc
                     && x.Status == (int)TransferStatus.Executed)
            .GroupBy(x => x.DestinationAddressId)
            .Select(g => new
            {
                AddressId = g.Key,
                Amount = g.Sum(x => x.AllocatedKwh)
            })
            .ToListAsync(ct);

        var outDict = transferredOut.ToDictionary(x => x.AddressId, x => x.Amount);
        var inDict = transferredIn.ToDictionary(x => x.AddressId, x => x.Amount);

        var positions = dailyBalances
            .Select(x => new AddressTransferPosition
            {
                AddressId = x.AddressId,
                DailySurplusKwh = x.DailySurplusKwh,
                DailyDeficitKwh = x.DailyDeficitKwh,
                AlreadyTransferredOutKwh = outDict.TryGetValue(x.AddressId, out var outAmount) ? outAmount : 0m,
                AlreadyTransferredInKwh = inDict.TryGetValue(x.AddressId, out var inAmount) ? inAmount : 0m
            })
            .ToList();

        return positions;
    }

///////////////////////////////////////////////////////////////////////
/// //Helpers

    private static DateTime ToUtcStartOfDay(DateOnly day) =>
        DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

private static List<TransferAllocation> MergeAllocations(
    List<TransferAllocation> allocations)
{
    return allocations
        .GroupBy(x => new { x.SourceAddressId, x.DestinationAddressId })
        .Select(g => new TransferAllocation
        {
            SourceAddressId = g.Key.SourceAddressId,
            DestinationAddressId = g.Key.DestinationAddressId,
            RequestedKwh = decimal.Round(g.Sum(x => x.RequestedKwh), 4),
            AllocatedKwh = decimal.Round(g.Sum(x => x.AllocatedKwh), 4),
            TransferRuleId = g.First().TransferRuleId,
            AppliedDistributionMode = g.First().AppliedDistributionMode
        })
        .ToList();
}


private TransferDistributionMode ResolveModeForSource(
    List<TransferRule> sourceRules)
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
}
