using Repositories.CRUD.Interfaces;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface ITransferRuleRepository : IRepository<DestinationTransferRule> { }

public class TransferRuleRepository : Repository<DestinationTransferRule>, ITransferRuleRepository
{
    public TransferRuleRepository(VnmDbContext context) : base(context) { }
}
