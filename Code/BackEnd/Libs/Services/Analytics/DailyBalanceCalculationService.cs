using Microsoft.EntityFrameworkCore;
using Repositories.Models;

namespace EnergyManagement.Services.Analytics;

public class DailyBalanceCalculationService : IDailyBalanceCalculationService
{
    private readonly VnmDbContext _db;
    private const decimal QuarterHourInHours = 0.25m;

    public DailyBalanceCalculationService(VnmDbContext dbContext)
    {
        _db = dbContext;
    }

    public async Task<DailyEnergyBalance> CalculateDailyBalancesAsync(
        int inverterInfoId,
        DateOnly day,
        CancellationToken ct = default)
    {
        var dayStart = day.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        var produced = (await _db.InverterReadings
            .Where(x => x.InverterInfoId == inverterInfoId
                     && x.Timestamp >= dayStart
                     && x.Timestamp < dayEnd)
            .SumAsync(x => (decimal?)x.Power * QuarterHourInHours / 1000m, ct)) ?? 0m;

        var consumed = (await _db.ConsumptionReadings
            .Where(x => x.InverterInfoId == inverterInfoId
                     && x.Timestamp >= dayStart
                     && x.Timestamp < dayEnd)
            .SumAsync(x => (decimal?)x.Power * QuarterHourInHours / 1000m, ct)) ?? 0m;

        var net = produced - consumed;

        var balance = await _db.DailyEnergyBalances
                        .FirstOrDefaultAsync(
                                x => x.InverterInfoId == inverterInfoId
                                    && x.Day.HasValue
                                    && x.Day.Value == dayStart,
                                ct);

        if (balance == null)
        {
            balance = new DailyEnergyBalance
            {
                InverterInfoId = inverterInfoId,
                Day = dayStart
            };

            _db.DailyEnergyBalances.Add(balance);
        }

        balance.ProducedKwh = produced;
        balance.ConsumedKwh = consumed;
        balance.NetKwh = net;
        balance.SurplusKwh = net > 0 ? net : 0m;
        balance.DeficitKwh = net < 0 ? Math.Abs(net) : 0m;
        balance.CalculatedAtUtc = DateTime.UtcNow;
        balance.Status = day == DateOnly.FromDateTime(DateTime.UtcNow)
            ? "Computed"
            : "Final";

        await _db.SaveChangesAsync(ct);

        return balance;
    }

    public async Task<IReadOnlyList<DailyEnergyBalance>> CalculateDailyBalancesForAllInvertersAsync(
        DateOnly day,
        CancellationToken ct = default)
    {
        var inverters = await _db.Addresses
            .Select(a => a.InverterInfos)
            .ToListAsync(ct);

        var result = new List<DailyEnergyBalance>();

        foreach (var inverterInfos in inverters)
        {
            foreach (var inverterInfo in inverterInfos)
            {
                var balance = await CalculateDailyBalancesAsync(inverterInfo.Id, day, ct);
                result.Add(balance);
            }
        }

        return result;
    }
}