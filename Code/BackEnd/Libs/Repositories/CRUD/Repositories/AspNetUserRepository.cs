using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Interfaces;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface IAspNetUserRepository : IRepository<AspNetUser>
{
    Task<AspNetUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<AspNetUser?> GetByExternalSubjectIdAsync(string externalSubjectId, CancellationToken cancellationToken = default);
}

public class AspNetUserRepository : Repository<AspNetUser>, IAspNetUserRepository
{
    private readonly VnmDbContext _context;

    public AspNetUserRepository(VnmDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<AspNetUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        return await _context.AspNetUsers.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
    }

    public async Task<AspNetUser?> GetByExternalSubjectIdAsync(string externalSubjectId, CancellationToken cancellationToken = default)
    {
        return await _context.AspNetUsers.AsNoTracking().FirstOrDefaultAsync(u => u.ExternalSubjectId == externalSubjectId, cancellationToken);
    }
}
