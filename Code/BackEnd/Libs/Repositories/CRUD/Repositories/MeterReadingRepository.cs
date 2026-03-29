using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Interfaces;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface IConsumptionReadingRepository : IRepository<ConsumptionReading>
{
    Task<IEnumerable<ConsumptionReading>> GetLatestReadingsAsync(int count, CancellationToken cancellationToken = default);
}

public class ConsumptionReadingRepository : Repository<ConsumptionReading>, IConsumptionReadingRepository
{
    private readonly VnmDbContext _context;

    public ConsumptionReadingRepository(VnmDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ConsumptionReading>> GetLatestReadingsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ConsumptionReading>()
            .AsNoTracking()
            .OrderByDescending(r => r.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}