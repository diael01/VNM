using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using DashboardBff.Services.Auth;

namespace DashboardBff.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/login", async (HttpContext httpContext, string? returnUrl) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = string.IsNullOrWhiteSpace(returnUrl)
                    ? "http://localhost:5173"
                    : returnUrl
            };

            await httpContext.ChallengeAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                properties);
        });

        endpoints.MapGet("/logout", async (HttpContext httpContext) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "http://localhost:5173"
            };

            await httpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            await httpContext.SignOutAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                properties);
        });

        endpoints.MapGet("/api/auth/me", (HttpContext httpContext, IAuthService authService) =>
        {
            if (!(httpContext.User.Identity?.IsAuthenticated ?? false))
            {
                return Results.Unauthorized();
            }

            var currentUser = authService.GetCurrentUser(httpContext.User);
            return Results.Ok(currentUser);
        });

        return endpoints;
    }
}