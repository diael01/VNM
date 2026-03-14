using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Interfaces;
using Repositories.Data;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface IAspNetRoleClaimRepository : IRepository<AspNetRoleClaim>
{
    Task<IEnumerable<AspNetRoleClaim>> GetForRoleAsync(string roleId, CancellationToken cancellationToken = default);
}

public class AspNetRoleClaimRepository : Repository<AspNetRoleClaim>, IAspNetRoleClaimRepository
{
    private readonly VnmDbContext _context;

    public AspNetRoleClaimRepository(VnmDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AspNetRoleClaim>> GetForRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        return await _context.AspNetRoleClaims.AsNoTracking().Where(rc => rc.RoleId == roleId).ToListAsync(cancellationToken);
    }
}
