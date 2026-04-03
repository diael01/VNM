using Microsoft.EntityFrameworkCore;
using Repositories.Models;

/* =========================================================
   SERVICES - ANALYTICS
   ========================================================= */

namespace EnergyManagement.Services.Analytics;

public interface IDailyBalanceCalculationService
{
    Task<DailyEnergyBalance> CalculateDailyBalancesAsync(InverterInfo inverterInfo, DateOnly day, CancellationToken ct = default);
    Task<IReadOnlyList<DailyEnergyBalance>> CalculateDailyBalancesForAllInvertersAsync(DateOnly day, CancellationToken ct = default);
}

