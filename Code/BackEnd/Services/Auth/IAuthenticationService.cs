using Models.Auth;
using System.Security.Claims;

namespace Services.Auth;

public interface IAuthenticationService
{
    CurrentUserDto GetCurrentUser(ClaimsPrincipal user);
}