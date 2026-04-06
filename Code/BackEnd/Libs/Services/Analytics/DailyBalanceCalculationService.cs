using Repositories.Models;
namespace EnergyManagement.Services.Analytics;
using Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public class DailyBalanceCalculationService : IDailyBalanceCalculationService
{
    private readonly VnmDbContext _db;
    private readonly MeteringOptions _metering;

    public DailyBalanceCalculationService(
        VnmDbContext db,
        IOptions<MeteringOptions> meteringOptions)
    {
        _db = db;
        _metering = meteringOptions.Value;
    }

    private decimal IntervalHours =>
        (_metering.ReadingIntervalMinutes > 0
            ? _metering.ReadingIntervalMinutes
            : 5) / 60m;

    public async Task<IReadOnlyList<DailyEnergyBalance>> CalculateDailyBalancesForAllAddressesAsync(
    DateOnly day,
    CancellationToken ct = default)
{
    var result = new List<DailyEnergyBalance>();

    var addresses = await _db.Addresses
        .AsNoTracking()
        .ToListAsync(ct);

    foreach (var address in addresses)
    {
        var balance = await CalculateDailyBalanceForAddressAsync(address.Id, day, ct);
        result.Add(balance);
    }

    return result;
}

public async Task<DailyEnergyBalance> CalculateDailyBalanceForAddressAsync(
    int addressId,
    DateOnly day,
    CancellationToken ct = default)
{
    var dayStart = DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
    var dayEnd = dayStart.AddDays(1);

    var interval = IntervalHours;

    var produced = (await _db.InverterReadings
        .Where(x => x.InverterInfo.AddressId == addressId
                 && x.Timestamp >= dayStart
                 && x.Timestamp < dayEnd)
        .SumAsync(x => (decimal?)x.Power * interval / 1000m, ct)) ?? 0m;

    var consumed = (await _db.ConsumptionReadings
        .Where(x => x.AddressId == addressId
                 && x.Timestamp >= dayStart
                 && x.Timestamp < dayEnd)
        .SumAsync(x => (decimal?)x.Power * interval / 1000m, ct)) ?? 0m;

    var net = produced - consumed;
    var surplus = net > 0 ? net : 0m;
    var deficit = net < 0 ? Math.Abs(net) : 0m;

    var balance = await _db.DailyEnergyBalances
        .FirstOrDefaultAsync(
            x => x.AddressId == addressId &&
                 x.Day >= dayStart &&
                 x.Day < dayEnd,
            ct);

    if (balance == null)
    {
        balance = new DailyEnergyBalance
        {
            AddressId = addressId,
            Day = dayStart
        };

        _db.DailyEnergyBalances.Add(balance);
    }

    balance.ProducedKwh = produced;
    balance.ConsumedKwh = consumed;
    balance.NetKwh = net;
    balance.NetPerAddressKwh = net;
    balance.SurplusKwh = surplus;
    balance.DeficitKwh = deficit;
    balance.CalculatedAtUtc = DateTime.UtcNow;
    balance.Status = day == DateOnly.FromDateTime(DateTime.UtcNow)
        ? "Computed"
        : "Final";

    await _db.SaveChangesAsync(ct);

    return balance;
}

    public async Task<DailyEnergyBalance> CalculateDailyBalancesAsync(
        InverterInfo inverterInfo,
        DateOnly day,
        CancellationToken ct = default)
    {
    var dayStart = DateTime.SpecifyKind(
        day.ToDateTime(TimeOnly.MinValue),
        DateTimeKind.Utc);

    var dayEnd = dayStart.AddDays(1);

        var interval = IntervalHours;

        var produced = (await _db.InverterReadings
            .Where(x => x.InverterInfoId == inverterInfo.Id
                     && x.Timestamp >= dayStart
                     && x.Timestamp < dayEnd)
            .SumAsync(x => (decimal?)x.Power * interval / 1000m, ct)) ?? 0m;

        var consumed = (await _db.ConsumptionReadings
            .Where(x => x.AddressId == inverterInfo.AddressId
                     && x.Timestamp >= dayStart
                     && x.Timestamp < dayEnd)
            .SumAsync(x => (decimal?)x.Power * interval / 1000m, ct)) ?? 0m;

        var net = produced - consumed;

        var netPerAddress = await CalculateNetPerAddressAsync(
            inverterInfo.AddressId,
            dayStart,
            dayEnd,
            ct);

        var balance = await _db.DailyEnergyBalances
            .FirstOrDefaultAsync(
                x => x.InverterInfoId == inverterInfo.Id
                  && x.Day >= dayStart
                  && x.Day < dayEnd,
                ct);

        if (balance == null)
        {
            balance = new DailyEnergyBalance
            {
                InverterInfoId = inverterInfo.Id,
                AddressId = inverterInfo.AddressId,
                Day = dayStart
            };

            _db.DailyEnergyBalances.Add(balance);
        }
        else
        {
            balance.AddressId = inverterInfo.AddressId;
        }

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

    private async Task<decimal> CalculateNetPerAddressAsync(
        int addressId,
        DateTime dayStart,
        DateTime dayEnd,
        CancellationToken ct)
    {
        var interval = IntervalHours;

        var produced = (await _db.InverterReadings
            .Where(x => x.InverterInfo.AddressId == addressId
                     && x.Timestamp >= dayStart
                     && x.Timestamp < dayEnd)
            .SumAsync(x => (decimal?)x.Power * interval / 1000m, ct)) ?? 0m;

        var consumed = (await _db.ConsumptionReadings
            .Where(x => x.AddressId == addressId
                     && x.Timestamp >= dayStart
                     && x.Timestamp < dayEnd)
            .SumAsync(x => (decimal?)x.Power * interval / 1000m, ct)) ?? 0m;

        return produced - consumed;
    }
}