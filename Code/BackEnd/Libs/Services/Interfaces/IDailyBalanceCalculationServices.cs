using Microsoft.EntityFrameworkCore;
using Repositories.Models;

/* =========================================================
   SERVICES - ANALYTICS
   ========================================================= */

namespace EnergyManagement.Services.Analytics;

public interface IDailyBalanceCalculationService
{
    Task<DailyEnergyBalance> CalculateDailyBalancesAsync(int addressId, DateOnly day, CancellationToken ct = default);
    Task<IReadOnlyList<DailyEnergyBalance>> CalculateDailyBalancesForAllAddressesAsync(DateOnly day, CancellationToken ct = default);
}

