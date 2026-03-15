using System.Security.Claims;
using Repositories.Models;
using Services.Auth;
using Services.Identity;
using Xunit;

namespace BackEnd.Tests.Unit.Repositories;

public class DbUserPermissionResolverTests
{
    [Fact]
    public async Task GetPermissionsAsync_AutoProvisionsUser_WhenMissingAndSubjectPresent()
    {
        var fakeService = new FakeAspNetIdentityService
        {
            UserByExternalSubjectId = null,
            UserByUserName = null,
            CreatedUserFactory = user =>
            {
                user.Id = "u-new";
                return user;
            },
            EffectiveClaims =
            [
                ("permission", "dashboard:read"),
                ("Permission", "dashboard:read"),
                ("permission", "dashboard:retry"),
                ("permission", "")
            ]
        };

        var resolver = new DbUserPermissionResolver(fakeService);
        var principal = BuildPrincipal(
            ("sub", "sub-123"),
            ("preferred_username", "alice"),
            ("email", "alice@example.com"));

        var permissions = await resolver.GetPermissionsAsync(principal);

        Assert.Equal(2, permissions.Count);
        Assert.Contains("dashboard:read", permissions);
        Assert.Contains("dashboard:retry", permissions);

        var createdUser = Assert.Single(fakeService.CreatedUsers);
        Assert.Equal("sub-123", createdUser.ExternalSubjectId);
        Assert.Equal("alice", createdUser.UserName);
        Assert.Equal("alice@example.com", createdUser.Email);
    }

    [Fact]
    public async Task GetPermissionsAsync_FallsBackToUsername_WhenSubjectMissing()
    {
        var existingUser = new AspNetUser { Id = "u-1", UserName = "bob" };
        var fakeService = new FakeAspNetIdentityService
        {
            UserByUserName = existingUser,
            EffectiveClaims = [("permission", "dashboard:read")]
        };

        var resolver = new DbUserPermissionResolver(fakeService);
        var principal = BuildPrincipal(("preferred_username", "bob"));

        var permissions = await resolver.GetPermissionsAsync(principal);

        Assert.Single(permissions);
        Assert.Contains("dashboard:read", permissions);
        Assert.Equal(0, fakeService.GetUserByExternalSubjectIdCalls);
        Assert.Equal(1, fakeService.GetUserByUserNameCalls);
        Assert.Empty(fakeService.CreatedUsers);
    }

    [Fact]
    public async Task GetPermissionsAsync_SyncsRolesFromIdentityProvider()
    {
        var existingUser = new AspNetUser { Id = "u-2", UserName = "charlie" };
        var localAdmin = new AspNetRole { Id = "r-admin", Name = "admin" };
        var localOld = new AspNetRole { Id = "r-old", Name = "old-role" };
        var createdContributors = new AspNetRole { Id = "r-contrib", Name = "contributors" };

        var fakeService = new FakeAspNetIdentityService
        {
            UserByExternalSubjectId = existingUser,
            UserRoles = [localAdmin, localOld],
            RoleByNameFactory = roleName =>
                roleName.Equals("admin", StringComparison.OrdinalIgnoreCase) ? localAdmin : null,
            CreatedRoleFactory = _ => createdContributors
        };

        var resolver = new DbUserPermissionResolver(fakeService);
        var principal = BuildPrincipal(
            ("sub", "sub-456"),
            (ClaimTypes.Role, "admin"),
            ("role", "contributors"));

        await resolver.GetPermissionsAsync(principal);

        Assert.Contains(("u-2", "r-old"), fakeService.RemovedRoles);
        Assert.DoesNotContain(("u-2", "r-admin"), fakeService.AssignedRoles);
        Assert.Contains(("u-2", "r-contrib"), fakeService.AssignedRoles);
        Assert.Single(fakeService.CreatedRoles);
        Assert.Equal("contributors", fakeService.CreatedRoles[0].Name);
    }

    private static ClaimsPrincipal BuildPrincipal(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private sealed class FakeAspNetIdentityService : IAspNetIdentityService
    {
        public AspNetUser? UserByExternalSubjectId { get; set; }
        public AspNetUser? UserByUserName { get; set; }
        public Func<AspNetUser, AspNetUser>? CreatedUserFactory { get; set; }
        public List<AspNetUser> CreatedUsers { get; } = [];

        public List<AspNetRole> UserRoles { get; set; } = [];
        public Func<string, AspNetRole?>? RoleByNameFactory { get; set; }
        public Func<AspNetRole, AspNetRole>? CreatedRoleFactory { get; set; }
        public List<AspNetRole> CreatedRoles { get; } = [];

        public List<(string userId, string roleId)> AssignedRoles { get; } = [];
        public List<(string userId, string roleId)> RemovedRoles { get; } = [];

        public List<(string ClaimType, string ClaimValue)> EffectiveClaims { get; set; } = [];

        public int GetUserByExternalSubjectIdCalls { get; private set; }
        public int GetUserByUserNameCalls { get; private set; }

        public Task<AspNetRole> CreateRoleAsync(AspNetRole role, CancellationToken cancellationToken = default)
        {
            var created = CreatedRoleFactory is not null ? CreatedRoleFactory(role) : role;
            CreatedRoles.Add(created);
            return Task.FromResult(created);
        }

        public Task<AspNetRole?> GetRoleByIdAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IEnumerable<AspNetRole>> GetAllRolesAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<AspNetRole?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
            => Task.FromResult(RoleByNameFactory?.Invoke(roleName));

        public Task<AspNetRole> UpdateRoleAsync(AspNetRole role, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> DeleteRoleAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<AspNetRoleClaim> CreateRoleClaimAsync(AspNetRoleClaim claim, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IEnumerable<AspNetRoleClaim>> GetClaimsByRoleIdAsync(string roleId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<AspNetUser> CreateUserAsync(AspNetUser user, CancellationToken cancellationToken = default)
        {
            var created = CreatedUserFactory is not null ? CreatedUserFactory(user) : user;
            CreatedUsers.Add(created);
            return Task.FromResult(created);
        }

        public Task<AspNetUser?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<AspNetUser?> GetUserByExternalSubjectIdAsync(string externalSubjectId, CancellationToken cancellationToken = default)
        {
            GetUserByExternalSubjectIdCalls++;
            return Task.FromResult(UserByExternalSubjectId);
        }

        public Task<AspNetUser?> GetUserByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            GetUserByUserNameCalls++;
            return Task.FromResult(UserByUserName);
        }

        public Task<IEnumerable<AspNetUser>> GetAllUsersAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<AspNetUser> UpdateUserAsync(AspNetUser user, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<AspNetUserClaim> CreateUserClaimAsync(AspNetUserClaim claim, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IEnumerable<AspNetUserClaim>> GetClaimsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> AssignRoleToUserAsync(string userId, string roleId, CancellationToken cancellationToken = default)
        {
            AssignedRoles.Add((userId, roleId));
            return Task.FromResult(true);
        }

        public Task<bool> RemoveRoleFromUserAsync(string userId, string roleId, CancellationToken cancellationToken = default)
        {
            RemovedRoles.Add((userId, roleId));
            return Task.FromResult(true);
        }

        public Task<IEnumerable<string>> GetUserRoleIdsAsync(string userId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IEnumerable<AspNetRole>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AspNetRole>>(UserRoles);

        public Task<IEnumerable<(string ClaimType, string ClaimValue)>> GetEffectiveUserClaimsAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<(string ClaimType, string ClaimValue)>>(EffectiveClaims);
    }
}
