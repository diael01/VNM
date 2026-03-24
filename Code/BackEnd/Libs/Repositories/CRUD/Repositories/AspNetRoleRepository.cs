using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Interfaces;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface IAspNetRoleRepository : IRepository<AspNetRole>
{
    Task<AspNetRole?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);
}

public class AspNetRoleRepository : Repository<AspNetRole>, IAspNetRoleRepository
{
    private readonly VnmDbContext _context;

    public AspNetRoleRepository(VnmDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<AspNetRole?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await _context.AspNetRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
    }
}
