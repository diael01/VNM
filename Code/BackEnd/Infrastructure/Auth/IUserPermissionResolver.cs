using System.Security.Claims;

namespace VNM.Infrastructure.Auth;

public interface IUserPermissionResolver
{
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
