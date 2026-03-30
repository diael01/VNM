using Microsoft.EntityFrameworkCore;
using Repositories.Models;

/* =========================================================
   SERVICES - ANALYTICS
   ========================================================= */

namespace EnergyManagement.Services.Analytics;

public interface IDailyBalanceCalculationService
{
    Task<DailyEnergyBalance> CalculateForAddressAsync(int addressId, DateOnly day, CancellationToken ct = default);
    Task<IReadOnlyList<DailyEnergyBalance>> CalculateForAllAddressesAsync(DateOnly day, CancellationToken ct = default);
}

