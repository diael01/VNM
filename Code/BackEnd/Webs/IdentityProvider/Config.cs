using Duende.IdentityModel;
using Duende.IdentityServer.Models;

namespace Globomantics.Idp;
//using IdentityModel;

//namespace VNM.IdentityProvider;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResource(
                name: "roles",
                displayName: "User roles",
                userClaims: new[] { JwtClaimTypes.Role })
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new[]
        {
            new ApiScope("energymanagement.read", "Read access to EnergyManagementService"),
            new ApiScope("inverter.read", "Read access to Simulators")
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new[]
        {
            new ApiResource("energymanagement-api", "Energy Management API")
            {
                Scopes = { "energymanagement.read" },
                UserClaims = { JwtClaimTypes.Name, JwtClaimTypes.Role }
            },
            new ApiResource("inverter-api", "Inverter API")
            {
                Scopes = { "inverter.read" },
                UserClaims = { JwtClaimTypes.Name, JwtClaimTypes.Role }
            }
        };

    public static IEnumerable<Client> Clients =>
        new[]
        {
            new Client
            {
                ClientId = "vnm-dashboard-bff",
                ClientName = "VNM Dashboard BFF",

                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = true,

                ClientSecrets =
                {
                    new Secret("super-secret-value".Sha256())
                },

                RedirectUris =
                {
                    "https://localhost:7144/signin-oidc"
                },

                PostLogoutRedirectUris =
                {
                    "https://localhost:7144/signout-callback-oidc"
                },

                AllowedScopes =
                {
                    "openid",
                    "profile",
                    "roles",
                    "energymanagement.read",
                    "inverter.read"
                },

                AllowOfflineAccess = true
            },
            new Client
            {
                ClientId = "vnm-meter-ingestion",
                ClientName = "VNM EnergyManagement Service",

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                RequireClientSecret = true,

                ClientSecrets =
                {
                    new Secret("meter-ingestion-secret".Sha256())
                },

                AllowedScopes =
                {
                    "inverter.read"
                }
            }
        };
}