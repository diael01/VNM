/* =========================================================
   SERVICES - TRANSFERS
   ========================================================= */

using Microsoft.EntityFrameworkCore;
using Repositories.Models;

namespace EnergyManagement.Services.Transfers
{
  


    public class AvailableBalanceService : IAvailableBalanceService
    {
        private readonly VnmDbContext _dbContext;

        public AvailableBalanceService(VnmDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AvailableTransferBalanceDto> GetAvailableBalanceAsync(int addressId, DateOnly day, CancellationToken ct = default)
        {
            var settlement = await _dbContext.ProviderSettlements
                .FirstOrDefaultAsync(x => x.LocationId == addressId && x.Day.HasValue && DateOnly.FromDateTime(x.Day.Value) == day);

            if (settlement == null)
                return new AvailableTransferBalanceDto();

            return new AvailableTransferBalanceDto
            {
                AddressId = addressId,
                Day = day,
                AvailableMoney = (decimal)(settlement.MonetaryCredit ?? 0),
                AvailableKwh = (decimal)(settlement.EnergyCreditKwh ?? 0)
            };
        }
    }
}