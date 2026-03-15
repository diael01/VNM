using Services.Identity;
using System.Security.Claims;
using VNM.Infrastructure.Auth;

namespace Services.Auth;

public sealed class DbUserPermissionResolver : IUserPermissionResolver
{
    private readonly IAspNetIdentityService _identityService;

    public DbUserPermissionResolver(IAspNetIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var subjectId = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = principal.FindFirst("preferred_username")?.Value
            ?? principal.Identity?.Name;

        var user = !string.IsNullOrWhiteSpace(subjectId)
            ? await _identityService.GetUserByExternalSubjectIdAsync(subjectId, cancellationToken)
            : null;

        if (user is null && !string.IsNullOrWhiteSpace(userName))
        {
            user = await _identityService.GetUserByUserNameAsync(userName, cancellationToken);
        }

        // Auto-provision: first time this user logs in, create a local record so an admin can assign roles later.
        if (user is null && !string.IsNullOrWhiteSpace(subjectId))
        {
            var email = principal.FindFirst("email")?.Value
                ?? principal.FindFirst(ClaimTypes.Email)?.Value;

            user = await _identityService.CreateUserAsync(new Repositories.Models.AspNetUser
            {
                Id = Guid.NewGuid().ToString(),
                ExternalSubjectId = subjectId,
                UserName = userName ?? subjectId,
                Email = email ?? string.Empty,
                PhoneNumber = string.Empty
            }, cancellationToken);
        }

        if (user is null)
        {
            return Array.Empty<string>();
        }

        await SyncRolesFromIdentityProviderAsync(user.Id, principal, cancellationToken);

        var effectiveClaims = await _identityService.GetEffectiveUserClaimsAsync(user.Id, cancellationToken);
        return effectiveClaims
            .Where(c => string.Equals(c.ClaimType, "permission", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.ClaimValue)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task SyncRolesFromIdentityProviderAsync(
        string userId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var idpRoles = principal.Claims
            .Where(c =>
                c.Type == ClaimTypes.Role ||
                c.Type == "role" ||
                c.Type == "roles" ||
                c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .Select(c => c.Value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var localRoles = (await _identityService.GetUserRolesAsync(userId, cancellationToken)).ToArray();

        // Remove local roles no longer present in IDP claims.
        foreach (var localRole in localRoles)
        {
            if (!idpRoles.Contains(localRole.Name, StringComparer.OrdinalIgnoreCase))
            {
                await _identityService.RemoveRoleFromUserAsync(userId, localRole.Id, cancellationToken);
            }
        }

        // Ensure each IDP role exists locally and is assigned to the user.
        foreach (var roleName in idpRoles)
        {
            var role = await _identityService.GetRoleByNameAsync(roleName, cancellationToken)
                ?? await _identityService.CreateRoleAsync(new Repositories.Models.AspNetRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName
                }, cancellationToken);

            if (!localRoles.Any(r => string.Equals(r.Id, role.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await _identityService.AssignRoleToUserAsync(userId, role.Id, cancellationToken);
            }
        }
    }
}
