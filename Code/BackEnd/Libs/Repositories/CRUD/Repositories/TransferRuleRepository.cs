using Repositories.CRUD.Interfaces;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface ITransferRuleRepository : IRepository<TransferRule> { }

public class TransferRuleRepository : Repository<TransferRule>, ITransferRuleRepository
{
    public TransferRuleRepository(VnmDbContext context) : base(context) { }
}
