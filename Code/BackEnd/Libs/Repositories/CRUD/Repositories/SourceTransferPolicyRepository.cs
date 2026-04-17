using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Interfaces;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface ISourceTransferPolicyRepository : IRepository<SourceTransferPolicy>
{
    Task<IEnumerable<SourceTransferPolicy>> GetAllWithCountsAsync(CancellationToken cancellationToken = default);
    Task<SourceTransferPolicy?> GetByIdWithChildrenAsync(int id, CancellationToken cancellationToken = default);
}

public class SourceTransferPolicyRepository : Repository<SourceTransferPolicy>, ISourceTransferPolicyRepository
{
    private readonly VnmDbContext _context;

    public SourceTransferPolicyRepository(VnmDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SourceTransferPolicy>> GetAllWithCountsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SourceTransferPolicies
            .AsNoTracking()
            .Include(p => p.DestinationTransferRules)
            .Include(p => p.SourceTransferSchedules)
            .ToListAsync(cancellationToken);
    }

    public async Task<SourceTransferPolicy?> GetByIdWithChildrenAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.SourceTransferPolicies
            .AsNoTracking()
            .Include(p => p.DestinationTransferRules)
            .Include(p => p.SourceTransferSchedules)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
