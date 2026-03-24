using Repositories.CRUD.Repositories;
using Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Services.Identity;

public interface IAspNetIdentityService
{
    Task<AspNetRole> CreateRoleAsync(AspNetRole role, CancellationToken cancellationToken = default);
    Task<AspNetRole?> GetRoleByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AspNetRole>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<AspNetRole?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<AspNetRole> UpdateRoleAsync(AspNetRole role, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoleAsync(string id, CancellationToken cancellationToken = default);

    Task<AspNetRoleClaim> CreateRoleClaimAsync(AspNetRoleClaim claim, CancellationToken cancellationToken = default);
    Task<IEnumerable<AspNetRoleClaim>> GetClaimsByRoleIdAsync(string roleId, CancellationToken cancellationToken = default);

    Task<AspNetUser> CreateUserAsync(AspNetUser user, CancellationToken cancellationToken = default);
    Task<AspNetUser?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<AspNetUser?> GetUserByExternalSubjectIdAsync(string externalSubjectId, CancellationToken cancellationToken = default);
    Task<AspNetUser?> GetUserByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<IEnumerable<AspNetUser>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<AspNetUser> UpdateUserAsync(AspNetUser user, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default);

    Task<AspNetUserClaim> CreateUserClaimAsync(AspNetUserClaim claim, CancellationToken cancellationToken = default);
    Task<IEnumerable<AspNetUserClaim>> GetClaimsByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> AssignRoleToUserAsync(string userId, string roleId, CancellationToken cancellationToken = default);
    Task<bool> RemoveRoleFromUserAsync(string userId, string roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetUserRoleIdsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AspNetRole>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<(string ClaimType, string ClaimValue)>> GetEffectiveUserClaimsAsync(string userId, CancellationToken cancellationToken = default);
}

public class AspNetIdentityService : IAspNetIdentityService
{
    private readonly IAspNetRoleRepository _roleRepository;
    private readonly IAspNetRoleClaimRepository _roleClaimRepository;
    private readonly IAspNetUserRepository _userRepository;
    private readonly IAspNetUserClaimRepository _userClaimRepository;
    private readonly VnmDbContext _context;

    public AspNetIdentityService(
        IAspNetRoleRepository roleRepository,
        IAspNetRoleClaimRepository roleClaimRepository,
        IAspNetUserRepository userRepository,
        IAspNetUserClaimRepository userClaimRepository,
        VnmDbContext context)
    {
        _roleRepository = roleRepository;
        _roleClaimRepository = roleClaimRepository;
        _userRepository = userRepository;
        _userClaimRepository = userClaimRepository;
        _context = context;
    }

    public async Task<AspNetRole> CreateRoleAsync(AspNetRole role, CancellationToken cancellationToken = default)
    {
        return await _roleRepository.AddAsync(role, cancellationToken);
    }

    public async Task<AspNetRole?> GetRoleByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _roleRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<AspNetRole>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _roleRepository.GetAllAsync(cancellationToken);
    }

    public async Task<AspNetRole?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await _roleRepository.GetByNameAsync(roleName, cancellationToken);
    }

    public async Task<AspNetRole> UpdateRoleAsync(AspNetRole role, CancellationToken cancellationToken = default)
    {
        return await _roleRepository.UpdateAsync(role, cancellationToken);
    }

    public async Task<bool> DeleteRoleAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _roleRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<AspNetRoleClaim> CreateRoleClaimAsync(AspNetRoleClaim claim, CancellationToken cancellationToken = default)
    {
        return await _roleClaimRepository.AddAsync(claim, cancellationToken);
    }

    public async Task<IEnumerable<AspNetRoleClaim>> GetClaimsByRoleIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        return await _roleClaimRepository.GetForRoleAsync(roleId, cancellationToken);
    }

    public async Task<AspNetUser> CreateUserAsync(AspNetUser user, CancellationToken cancellationToken = default)
    {
        return await _userRepository.AddAsync(user, cancellationToken);
    }

    public async Task<AspNetUser?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<AspNetUser?> GetUserByExternalSubjectIdAsync(string externalSubjectId, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByExternalSubjectIdAsync(externalSubjectId, cancellationToken);
    }

    public async Task<AspNetUser?> GetUserByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByUserNameAsync(userName, cancellationToken);
    }

    public async Task<IEnumerable<AspNetUser>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetAllAsync(cancellationToken);
    }

    public async Task<AspNetUser> UpdateUserAsync(AspNetUser user, CancellationToken cancellationToken = default)
    {
        return await _userRepository.UpdateAsync(user, cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _userRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<AspNetUserClaim> CreateUserClaimAsync(AspNetUserClaim claim, CancellationToken cancellationToken = default)
    {
        return await _userClaimRepository.AddAsync(claim, cancellationToken);
    }

    public async Task<IEnumerable<AspNetUserClaim>> GetClaimsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _userClaimRepository.GetForUserAsync(userId, cancellationToken);
    }

    public async Task<bool> AssignRoleToUserAsync(string userId, string roleId, CancellationToken cancellationToken = default)
    {
        var user = await _context.AspNetUsers.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return false;
        var role = await _context.AspNetRoles.FindAsync(new object[] { roleId }, cancellationToken);
        if (role == null) return false;

        if (!user.Roles.Any(r => r.Id == roleId))
        {
            user.Roles.Add(role);
            await _context.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    public async Task<bool> RemoveRoleFromUserAsync(string userId, string roleId, CancellationToken cancellationToken = default)
    {
        var user = await _context.AspNetUsers.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return false;
        var role = user.Roles.FirstOrDefault(r => r.Id == roleId);
        if (role == null) return false;

        user.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<string>> GetUserRoleIdsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.AspNetUsers.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return Enumerable.Empty<string>();
        return user.Roles.Select(r => r.Id);
    }

    public async Task<IEnumerable<AspNetRole>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.AspNetUsers.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return Enumerable.Empty<AspNetRole>();
        return user.Roles;
    }

    public async Task<IEnumerable<(string ClaimType, string ClaimValue)>> GetEffectiveUserClaimsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.AspNetUsers
            .Include(u => u.Roles)
            .ThenInclude(r => r.AspNetRoleClaims)
            .Include(u => u.AspNetUserClaims)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return Enumerable.Empty<(string, string)>();

        var claims = new List<(string, string)>();
        claims.AddRange(user.AspNetUserClaims.Select(c => (c.ClaimType ?? string.Empty, c.ClaimValue ?? string.Empty)));
        claims.AddRange(user.Roles.SelectMany(r => r.AspNetRoleClaims).Select(c => (c.ClaimType ?? string.Empty, c.ClaimValue ?? string.Empty)));
        return claims.Distinct();
    }
}
