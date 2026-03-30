/* =========================================================
   SERVICES - PROVIDER
   ========================================================= */

using Microsoft.EntityFrameworkCore;
using Repositories.Models;

namespace EnergyManagement.Services.Providers
{

    public class ProviderSettlementService : IProviderSettlementService
    {
        private readonly VnmDbContext _db;       
        private readonly ISettlementModeResolver _resolver;

        public ProviderSettlementService(VnmDbContext db, ISettlementModeResolver resolver)
        {
            _db = db;
            _resolver = resolver;
        }

         public async Task<ProviderSettlement> ProcessSettlementAsync(int addressId, DateOnly day, CancellationToken ct)
        {   
            var balance = await _db.DailyEnergyBalances
                .FirstAsync(x => x.LocationId == addressId && x.Day.HasValue && DateOnly.FromDateTime(x.Day.Value) == day, ct);

            var mode = _resolver.GetCurrentMode();
            var strategy = _resolver.Resolve(mode);

            var settlement = new ProviderSettlement
            {
                LocationId = addressId,
                Day = day.ToDateTime(TimeOnly.MinValue)
            };

            strategy.FillSettlement(settlement, balance, 0.8m, 1.0m);

            _db.ProviderSettlements.Add(settlement);
            await _db.SaveChangesAsync(ct);

            return settlement;
        }

        public async Task<IReadOnlyList<ProviderSettlement>> ProcessSettlementsForAllAddressesAsync(DateOnly day, CancellationToken ct = default)
        {
            var ids = await _db.Addresses.Select(x => x.Id).ToListAsync(ct);

            var result = new List<ProviderSettlement>();

            foreach (var id in ids)
            {
                result.Add(await ProcessSettlementAsync(id, day, ct));
            }

            return result;
        }
    }
}
