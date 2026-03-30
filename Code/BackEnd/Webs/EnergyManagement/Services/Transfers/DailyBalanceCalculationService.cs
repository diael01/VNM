using Microsoft.EntityFrameworkCore;
using Repositories.Models;

/* =========================================================
   SERVICES - ANALYTICS
   ========================================================= */

namespace EnergyManagement.Services.Analytics
{

    public class DailyBalanceCalculationService : IDailyBalanceCalculationService
    {
        private readonly VnmDbContext _db;
        private const decimal QuarterHourInHours = 0.25m;

        public DailyBalanceCalculationService(VnmDbContext dbContext)
        {
            _db = dbContext;
        }

        public async Task<DailyEnergyBalance> CalculateForAddressAsync(int addressId, DateOnly day, CancellationToken ct)
        {
            var start = day.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);

           var produced = (await _db.InverterReadings
                .Where(x => x.Inverter.AddressId == addressId && x.Timestamp >= start && x.Timestamp < end)
                .SumAsync(x => (decimal?)x.Power * 0.25m / 1000m)) ?? 0m;

            var consumed = (await _db.ConsumptionReadings
                .Where(x => x.LocationId == addressId && x.Timestamp >= start && x.Timestamp < end)
                .SumAsync(x => (decimal?)x.Power * 0.25m / 1000m)) ?? 0m;

            var balance = await _db.DailyEnergyBalances
                    .FirstOrDefaultAsync(x => x.LocationId == addressId && x.Day.HasValue && x.Day.Value.Date == day.ToDateTime(TimeOnly.MinValue).Date);

            if (balance == null)
            {
                balance = new DailyEnergyBalance
                {
                    LocationId = addressId,
                    Day = day.ToDateTime(TimeOnly.MinValue)
                };
                _db.DailyEnergyBalances.Add(balance);
            }

            balance.ProducedKwh = produced;
            balance.ConsumedKwh = consumed;
            balance.SurplusKwh = Math.Max(produced - consumed, 0m);
            balance.DeficitKwh = Math.Max(consumed - produced, 0m);
            balance.CalculatedAtUtc = DateTime.UtcNow;
            balance.Status = "Final";

            await _db.SaveChangesAsync();

            return balance;
        }
/* 
          public async Task<DailyEnergyBalance> CalculateForAddressAsync(int addressId, DateOnly day, CancellationToken ct)
        {
            var start = day.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);

            var produced = await _db.InverterReadings
                .Where(x => x.Inverter.AddressId == addressId && x.Timestamp >= start && x.Timestamp < end)
                .SumAsync(x => (decimal)x.Power * 0.25m / 1000m);

            var consumed = await _db.ConsumptionReadings
                .Where(x => x.LocationId == addressId && x.Timestamp >= start && x.Timestamp < end)
                .SumAsync(x => x.Power * 0.25m / 1000m);

            var balance = new DailyEnergyBalance
            {
                LocationId = addressId,
                Day = day.ToDateTime(TimeOnly.MinValue),
                ProducedKwh = produced,
                ConsumedKwh = consumed,
                SurplusKwh = Math.Max(produced - consumed, 0),
                DeficitKwh = Math.Max(consumed - produced, 0),
                CalculatedAtUtc = DateTime.UtcNow,
                Status = "Final"
            };

            _db.DailyEnergyBalances.Add(balance);
            await _db.SaveChangesAsync();

            return balance;
        }
 */
        public async Task<IReadOnlyList<DailyEnergyBalance>> CalculateForAllAddressesAsync(DateOnly day, CancellationToken ct = default)
        {
            var ids = await _db.Addresses.Select(a => a.Id).ToListAsync(ct);

            var result = new List<DailyEnergyBalance>();

            foreach (var id in ids)
            {
                result.Add(await CalculateForAddressAsync(id, day, ct));
            }

            return result;
        }
    }
}