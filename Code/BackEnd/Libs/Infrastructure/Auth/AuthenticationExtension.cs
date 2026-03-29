using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using VNM.Infrastructure.Auth;


namespace VNM.Infrastructure.Extensions;

/// <summary>
/// Provides service collection extensions for BFF authentication.
/// </summary>
public static class AuthenticationExtension
{
    /// <summary>
    /// Registers cookie and OpenID Connect authentication for the BFF.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBffAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication:Authority is missing.");

        var clientId = configuration["Authentication:ClientId"]
            ?? throw new InvalidOperationException("Authentication:ClientId is missing.");

        var clientSecret = configuration["Authentication:ClientSecret"]
            ?? throw new InvalidOperationException("Authentication:ClientSecret is missing.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "__Host-vnm-bff";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
           .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authority;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("roles");
                options.Scope.Add("energymanagement.read");
                options.Scope.Add("inverter.read");

                options.ClaimActions.MapUniqueJsonKey("role", "role");

                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";

                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";
                options.RequireHttpsMetadata = true;
            });

        services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Events ??= new OpenIdConnectEvents();
            var previousOnTokenValidated = options.Events.OnTokenValidated;

            options.Events.OnTokenValidated = async context =>
            {
                if (previousOnTokenValidated is not null)
                {
                    await previousOnTokenValidated(context);
                }

                if (context.Principal?.Identity is not ClaimsIdentity identity)
                {
                    return;
                }

                var resolver = context.HttpContext.RequestServices.GetService<IUserPermissionResolver>();
                if (resolver is null)
                {
                    return;
                }

                var knownPermissions = new HashSet<string>(
                    identity.FindAll("permission").Select(c => c.Value),
                    StringComparer.OrdinalIgnoreCase);

                var permissions = await resolver.GetPermissionsAsync(context.Principal, context.HttpContext.RequestAborted);
                foreach (var permission in permissions.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    if (knownPermissions.Add(permission))
                    {
                        identity.AddClaim(new Claim("permission", permission));
                    }
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("CanReadDashboard", policy =>
                policy.RequireClaim("permission", "dashboard:read"));
            options.AddPolicy("CanRetryDashboard", policy =>
                policy.RequireClaim("permission", "dashboard:retry"));
        });

        return services;
    }
}