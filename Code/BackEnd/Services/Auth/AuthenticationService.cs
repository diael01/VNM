using Models.Auth;
using System.Security.Claims;

namespace Services.Auth;

public sealed class AuthenticationService : IAuthenticationService
{
    public CurrentUserDto GetCurrentUser(ClaimsPrincipal user)
    {
        var name =
            user.FindFirst("name")?.Value ??
            user.FindFirst("preferred_username")?.Value ??
            user.FindFirst(ClaimTypes.Name)?.Value ??
            user.Identity?.Name;

        var roles = user.Claims
            .Where(c =>
                c.Type == ClaimTypes.Role ||
                c.Type == "role" ||
                c.Type == "roles" ||
                c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .Select(c => c.Value)
            .Distinct()
            .ToArray();

        var claims = user.Claims
            .Select(c => new ClaimDto
            {
                Type = c.Type,
                Value = c.Value
            })
            .ToArray();

        return new CurrentUserDto
        {
            Name = name,
            Roles = roles,
            Claims = claims
        };
    }
}