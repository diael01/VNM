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
        InverterInfo inverterInfo,
        DateOnly day,
        CancellationToken ct = default)
    {
        var dayStart = day.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        var produced = (await _db.InverterReadings
            .Where(x => x.InverterInfoId == inverterInfo.Id
                     && x.Timestamp >= dayStart
                     && x.Timestamp < dayEnd)
            .SumAsync(x => (decimal?)x.Power * QuarterHourInHours / 1000m, ct)) ?? 0m;

        var consumed = (await _db.ConsumptionReadings
            .Where(x => x.AddressId == inverterInfo.AddressId
                     && x.Timestamp >= dayStart
                     && x.Timestamp < dayEnd)
            .SumAsync(x => (decimal?)x.Power * QuarterHourInHours / 1000m, ct)) ?? 0m;

        var net = produced - consumed;

        var netPerAddress = await CalculateNetPerAddressAsync(inverterInfo.AddressId, dayStart, dayEnd, ct);
        var balance = await _db.DailyEnergyBalances
                        .FirstOrDefaultAsync(
                                x => x.InverterInfoId == inverterInfo.Id
                                    && DateOnly.FromDateTime(x.Day) == day,
                                ct);

        if (balance == null)
        {
            balance = new DailyEnergyBalance
            {
                InverterInfoId = inverterInfo.Id,
                Day = dayStart,
                AddressId = inverterInfo.AddressId,
                NetPerAddressKwh = netPerAddress
            };

            _db.DailyEnergyBalances.Add(balance);
        }
        else
            // in case old rows exist without address filled correctly
            balance.AddressId = inverterInfo.AddressId;

        balance.ProducedKwh = produced;
        balance.ConsumedKwh = consumed;
        balance.NetKwh = net;
        balance.NetPerAddressKwh = netPerAddress;
        balance.SurplusKwh = net > 0 ? net : 0m;
        balance.DeficitKwh = net < 0 ? Math.Abs(net) : 0m;
        balance.CalculatedAtUtc = DateTime.UtcNow;
        balance.Status = day == DateOnly.FromDateTime(DateTime.UtcNow)
            ? "Computed"
            : "Final";

        await _db.SaveChangesAsync(ct);

        return balance;
    }

private async Task<decimal> CalculateNetPerAddressAsync(int addressId, DateTime dayStart, DateTime dayEnd, CancellationToken ct)
{
    var producedPerAddress = (await _db.InverterReadings
        .Where(x => x.InverterInfo.AddressId == addressId
                 && x.Timestamp >= dayStart
                 && x.Timestamp < dayEnd)
        .SumAsync(x => (decimal?)x.Power * QuarterHourInHours / 1000m, ct)) ?? 0m;

    var consumedPerAddress = (await _db.ConsumptionReadings
        .Where(x => x.AddressId == addressId
                 && x.Timestamp >= dayStart
                 && x.Timestamp < dayEnd)
        .SumAsync(x => (decimal?)x.Power * QuarterHourInHours / 1000m, ct)) ?? 0m;

    return producedPerAddress - consumedPerAddress;
}

    public async Task<IReadOnlyList<DailyEnergyBalance>> CalculateDailyBalancesForAllInvertersAsync(
        DateOnly day,
        CancellationToken ct = default)
    {    
        var result = new List<DailyEnergyBalance>();

        foreach (var inverterInfo in _db.InverterInfos.AsNoTracking())
        {           
                var balance = await CalculateDailyBalancesAsync(inverterInfo, day, ct);
                result.Add(balance);            
        }

        return result;
    }
}