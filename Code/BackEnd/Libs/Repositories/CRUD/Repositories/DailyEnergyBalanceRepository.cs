using Repositories.CRUD.Interfaces;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface IDailyEnergyBalanceRepository : IRepository<DailyEnergyBalance> { }

public class DailyEnergyBalanceRepository : Repository<DailyEnergyBalance>, IDailyEnergyBalanceRepository
{
    public DailyEnergyBalanceRepository(VnmDbContext context) : base(context) { }
}
