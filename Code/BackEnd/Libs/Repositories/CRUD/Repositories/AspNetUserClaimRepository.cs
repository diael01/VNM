using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Interfaces;
using Repositories.Data;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface IAspNetUserClaimRepository : IRepository<AspNetUserClaim>
{
    Task<IEnumerable<AspNetUserClaim>> GetForUserAsync(string userId, CancellationToken cancellationToken = default);
}

public class AspNetUserClaimRepository : Repository<AspNetUserClaim>, IAspNetUserClaimRepository
{
    private readonly VnmDbContext _context;

    public AspNetUserClaimRepository(VnmDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AspNetUserClaim>> GetForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.AspNetUserClaims.AsNoTracking().Where(c => c.UserId == userId).ToListAsync(cancellationToken);
    }
}
