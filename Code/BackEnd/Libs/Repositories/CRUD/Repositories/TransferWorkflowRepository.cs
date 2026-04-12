using Repositories.CRUD.Interfaces;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface ITransferWorkflowRepository : IRepository<TransferWorkflow> { }

public class TransferWorkflowRepository : Repository<TransferWorkflow>, ITransferWorkflowRepository
{
    public TransferWorkflowRepository(VnmDbContext context) : base(context) { }
}
