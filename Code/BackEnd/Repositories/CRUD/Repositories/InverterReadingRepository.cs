using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Interfaces;
using Repositories.Data;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface IInverterReadingRepository : IRepository<InverterReading>
{
    Task<IEnumerable<InverterReading>> GetLatestReadingsAsync(int count, CancellationToken cancellationToken = default);
}

public class InverterReadingRepository : Repository<InverterReading>, IInverterReadingRepository
{
    private readonly VnmDbContext _context;

    public InverterReadingRepository(VnmDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InverterReading>> GetLatestReadingsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.InverterReadings
            .AsNoTracking()
            .OrderByDescending(r => r.TimestampUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
