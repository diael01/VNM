using Microsoft.EntityFrameworkCore;
using Repositories.Models;

namespace EnergyManagement.Services.Providers;

public interface IProviderSettlementService
{
    Task<ProviderSettlement> ProcessSettlementAsync(int addressId, DateOnly day, CancellationToken ct = default);
    Task<IReadOnlyList<ProviderSettlement>> ProcessSettlementsForAllAddressesAsync(DateOnly day, CancellationToken ct = default);
}
