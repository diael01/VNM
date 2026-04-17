using Repositories.CRUD.Interfaces;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface ISourceTransferScheduleRepository : IRepository<SourceTransferSchedule>
{
}

public class SourceTransferScheduleRepository : Repository<SourceTransferSchedule>, ISourceTransferScheduleRepository
{
    public SourceTransferScheduleRepository(VnmDbContext context) : base(context) { }
}
