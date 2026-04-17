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

        await DeleteExistingAutoPlannedRowsForSourceAsync(
            sourceAddressId,
            dayStartUtc,
            dayEndUtc,
            ct);

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
            return Array.Empty<TransferWorkflow>();

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
            return Array.Empty<TransferWorkflow>();

        var workflows = distributionMode switch
        {
            TransferDistributionMode.Fair =>
                AllocateFair(sourcePosition, destinations, dayStartUtc),

            TransferDistributionMode.Priority =>
                AllocatePriority(sourcePosition, destinations, dayStartUtc),

            TransferDistributionMode.Weighted =>
                AllocateWeighted(sourcePosition, destinations, dayStartUtc),

            _ => throw new InvalidOperationException("Unknown distribution mode")
        };

        if (workflows.Count > 0)
        {
            _db.TransferWorkflows.AddRange(workflows);
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Generated {Count} workflows for source {Source}",
            workflows.Count,
            sourceAddressId);

        return workflows;
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
        var dayUtc = ToUtcStartOfDay(request.Day);
        var workflows = new List<TransferWorkflow>();

        foreach (var target in request.Targets)
        {
            if (target.RequestedKwh <= 0)
                continue;

            if (target.DestinationAddressId == request.SourceAddressId)
                throw new InvalidOperationException("Source and destination cannot be the same address.");

            workflows.Add(new TransferWorkflow
            {
                SourceAddressId = request.SourceAddressId,
                DestinationAddressId = target.DestinationAddressId,
                AmountKwh = target.RequestedKwh,
                BalanceDayUtc = dayUtc,
                Status = (int)TransferStatus.Planned,
                TriggerType = (int)TriggerType.Manual
            });
        }

        if (workflows.Count > 0)
        {
            _db.TransferWorkflows.AddRange(workflows);
            await _db.SaveChangesAsync(ct);
        }

        return workflows;
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

        var share = totalSurplus / count;

        foreach (var dest in destinations)
        {
            var amount = Math.Min(share, dest.Position.RemainingDeficitKwh);

            if (amount <= 0)
                continue;

            workflows.Add(CreateWorkflow(
                source.AddressId,
                dest.Rule.DestinationAddressId,
                amount,
                dayUtc));
        }

        return workflows;
    }

    private static List<TransferWorkflow> AllocatePriority(
        AddressPosition source,
        List<DestinationEntry> destinations,
        DateTime dayUtc)
    {
        var workflows = new List<TransferWorkflow>();
        var remaining = source.RemainingSurplusKwh;

        foreach (var dest in destinations.OrderBy(x => x.Rule.Priority))
        {
            if (remaining <= 0)
                break;

            var amount = Math.Min(remaining, dest.Position.RemainingDeficitKwh);

            if (amount <= 0)
                continue;

            workflows.Add(CreateWorkflow(
                source.AddressId,
                dest.Rule.DestinationAddressId,
                amount,
                dayUtc));

            remaining -= amount;
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
        var totalWeight = destinations.Sum(x => x.Rule.WeightPercent ?? 0);

        if (totalWeight <= 0 || totalSurplus <= 0)
            return workflows;

        foreach (var dest in destinations)
        {
            var weight = dest.Rule.WeightPercent ?? 0;
            var portion = totalSurplus * (weight / totalWeight);
            var amount = Math.Min(portion, dest.Position.RemainingDeficitKwh);

            if (amount <= 0)
                continue;

            workflows.Add(CreateWorkflow(
                source.AddressId,
                dest.Rule.DestinationAddressId,
                amount,
                dayUtc));
        }

        return workflows;
    }

    private static TransferWorkflow CreateWorkflow(
        int sourceId,
        int destId,
        decimal amount,
        DateTime dayUtc)
    {
        return new TransferWorkflow
        {
            SourceAddressId = sourceId,
            DestinationAddressId = destId,
            AmountKwh = amount,
            BalanceDayUtc = dayUtc,
            Status = (int)TransferStatus.Planned,
            TriggerType = (int)TriggerType.Auto
        };
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
