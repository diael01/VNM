using Repositories.CRUD.Interfaces;
using Repositories.Data;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface IInverterInfoRepository : IRepository<InverterInfo> { }

public class InverterInfoRepository : Repository<InverterInfo>, IInverterInfoRepository
{
    public InverterInfoRepository(VnmDbContext context) : base(context) { }
}