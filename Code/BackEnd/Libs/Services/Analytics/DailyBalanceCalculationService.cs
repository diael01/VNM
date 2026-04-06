using Repositories.Models;
using Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnergyManagement.Services.Analytics;

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
            .Select(x => x.Id)
            .ToListAsync(ct);

        foreach (var addressId in addresses)
        {
            var balance = await CalculateDailyBalanceForAddressAsync(addressId, day, ct);
            result.Add(balance);
        }

        return result;
    }

    public async Task<DailyEnergyBalance> CalculateDailyBalanceForAddressAsync(
        int addressId,
        DateOnly day,
        CancellationToken ct = default)
    {
        var dayStart = DateTime.SpecifyKind(
            day.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

        var dayEnd = dayStart.AddDays(1);

        var interval = IntervalHours;

        // ✅ PRODUCED (no join anymore!)
        var produced = (await _db.InverterReadings
            .Where(x => x.AddressId == addressId
                     && x.Timestamp >= dayStart
                     && x.Timestamp < dayEnd)
            .SumAsync(x => (decimal?)x.Power * interval / 1000m, ct)) ?? 0m;

        // ✅ CONSUMED
        var consumed = (await _db.ConsumptionReadings
            .Where(x => x.AddressId == addressId
                     && x.Timestamp >= dayStart
                     && x.Timestamp < dayEnd)
            .SumAsync(x => (decimal?)x.Power * interval / 1000m, ct)) ?? 0m;

        var net = produced - consumed;

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
        balance.SurplusKwh = net > 0 ? net : 0m;
        balance.DeficitKwh = net < 0 ? Math.Abs(net) : 0m;
        balance.CalculatedAtUtc = DateTime.UtcNow;
        balance.Status = day == DateOnly.FromDateTime(DateTime.UtcNow)
            ? "Computed"
            : "Final";

        await _db.SaveChangesAsync(ct);

        return balance;
    }
}