using DashboardBff.Models.Auth;
using System.Security.Claims;

namespace DashboardBff.Services.Auth;

public interface IAuthService
{
    CurrentUserDto GetCurrentUser(ClaimsPrincipal user);
}