using Duende.IdentityModel;
using Duende.IdentityServer.Models;

namespace Globomantics.Idp;

public static class Config
{

    public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                 new IdentityResource("VNM", new [] { JwtClaimTypes.Role })
                //new IdentityResource(name: "roles",
                //    userClaims: new[] { "role" }, displayName: "Your roles")
            };



    public static IEnumerable<ApiResource> ApiResources =>
    new ApiResource[]
    {
            new ApiResource("DashboardApi")
            {
                Scopes = { "DashboardApi_fullaccess"},
                ApiSecrets = { new Secret("secret".Sha256()) },
            }
    };

    public static IEnumerable<ApiScope> ApiScopes =>
       new ApiScope[]
       {
                //new ApiScope("DashboardApi_fullaccess", "Basic access to Dashboard API"),
                new ApiScope("DashboardApi_fullaccess") { UserClaims = new[] {JwtClaimTypes.Email, JwtClaimTypes.Role } },
                new ApiScope("DashboardAuthorization")
       };


    public static IEnumerable<Client> Clients =>
        new Client[]
        {

            new Client
            {
                ClientId = "DashboardApi.client",
                ClientName = "Dashboard Api",
                 AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets =
                {
                    new Secret("secret".Sha256())
                },
                 AllowedScopes =
                {
                    "DashboardApi_fullaccess", "DashboardAuthorization"
                },
                Claims = new ClientClaim[]
                {
                    new ClientClaim("ClientType", "DashboardApi")
                }
        },
       

            // interactive client using code flow + pkce
            new Client
            {
                ClientId = "interactive",
                ClientSecrets = { new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:4002/signin-oidc" },
                FrontChannelLogoutUri = "https://localhost:4002/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:4002/signout-callback-oidc" },

                AllowOfflineAccess = true,
                AbsoluteRefreshTokenLifetime = 2592000, // 30 days
                SlidingRefreshTokenLifetime = 1209600, // 14 days

                Claims = new ClientClaim[]
                {
                    new ClientClaim("clienttype", "interactive")
                },

                AllowedScopes = { "openid", "profile", "Dashboard",
                    "DashboardApi_fullaccess", "DashboardAuthorization" },
            },
        };
}
