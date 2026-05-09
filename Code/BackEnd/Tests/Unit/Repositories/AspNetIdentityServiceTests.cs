using Microsoft.EntityFrameworkCore;
using Repositories.Models;
using Repositories.CRUD.Repositories;
using Services.Identity;
using Xunit;

namespace BackEnd.Tests.Unit.Repositories;

public class AspNetIdentityServiceTests
{
    private VnmDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<VnmDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new VnmDbContext(options);
    }

    private AspNetIdentityService CreateService(VnmDbContext context)
    {
        return new AspNetIdentityService(
            new AspNetRoleRepository(context),
            new AspNetRoleClaimRepository(context),
            new AspNetUserRepository(context),
            new AspNetUserClaimRepository(context),
            context);
    }

    [Fact]
    public async Task AspNetIdentityService_SeedDefaults_CreatesRolesUsersAndClaims()
    {
        using var context = CreateContext("AspNetIdentityService_SeedDefaults");
        var roleRepo = new AspNetRoleRepository(context);
        var roleClaimRepo = new AspNetRoleClaimRepository(context);
        var userRepo = new AspNetUserRepository(context);
        var userClaimRepo = new AspNetUserClaimRepository(context);
        var service = new AspNetIdentityService(roleRepo, roleClaimRepo, userRepo, userClaimRepo, context);

        var adminRole = await service.CreateRoleAsync(new AspNetRole { Id = "r-admin", Name = "admin" });
        var contributorsRole = await service.CreateRoleAsync(new AspNetRole { Id = "r-contributors", Name = "contributors" });
        await service.CreateRoleClaimAsync(new AspNetRoleClaim { RoleId = adminRole.Id, ClaimType = "permission", ClaimValue = "dashboard:retry" });
        await service.CreateRoleClaimAsync(new AspNetRoleClaim { RoleId = contributorsRole.Id, ClaimType = "permission", ClaimValue = "dashboard:read" });

        var aliceUser = await service.CreateUserAsync(new AspNetUser
        {
            Id = "u-alice",
            UserName = "alice",
            Email = "alice@example.com",
            PhoneNumber = "555-0100",
            ExternalSubjectId = "alice-subject"
        });
        var bobUser = await service.CreateUserAsync(new AspNetUser
        {
            Id = "u-bob",
            UserName = "bob",
            Email = "bob@example.com",
            PhoneNumber = "555-0200",
            ExternalSubjectId = "bob-subject"
        });

        await service.AssignRoleToUserAsync(aliceUser.Id, adminRole.Id);
        await service.AssignRoleToUserAsync(bobUser.Id, contributorsRole.Id);

        var roles = (await service.GetAllRolesAsync()).ToList();
        Assert.Contains(roles, r => r.Name == "admin");
        Assert.Contains(roles, r => r.Name == "contributors");

        var alice = await service.GetUserByUserNameAsync("alice");
        var bob = await service.GetUserByUserNameAsync("bob");
        Assert.NotNull(alice);
        Assert.NotNull(bob);

        var aliceClaims = (await service.GetEffectiveUserClaimsAsync(alice!.Id)).ToList();
        Assert.Contains(aliceClaims, c => c.ClaimType == "permission" && c.ClaimValue == "dashboard:retry");

        var bobClaims = (await service.GetEffectiveUserClaimsAsync(bob!.Id)).ToList();
        Assert.Contains(bobClaims, c => c.ClaimType == "permission" && c.ClaimValue == "dashboard:read");
        Assert.DoesNotContain(bobClaims, c => c.ClaimType == "permission" && c.ClaimValue == "dashboard:retry");
    }

    [Fact]
    public async Task AspNetIdentityService_AssignAndRemoveRole_Works()
    {
        using var context = CreateContext("AspNetIdentityService_AssignAndRemoveRole");
        var service = CreateService(context);

        var role = await service.CreateRoleAsync(new AspNetRole { Id = "r-x", Name = "test-role" });
        var user = await service.CreateUserAsync(new AspNetUser { Id = "u-x", UserName = "testuser", Email = "test@example.com", PhoneNumber = "555-0000", ExternalSubjectId = "tx" });

        var assigned = await service.AssignRoleToUserAsync(user.Id, role.Id);
        Assert.True(assigned);

        var roles = (await service.GetUserRolesAsync(user.Id)).ToList();
        Assert.Single(roles);
        Assert.Equal("test-role", roles[0].Name);

        var removed = await service.RemoveRoleFromUserAsync(user.Id, role.Id);
        Assert.True(removed);

        var rolesAfterRemove = (await service.GetUserRolesAsync(user.Id)).ToList();
        Assert.Empty(rolesAfterRemove);
    }

    [Fact]
    public async Task AspNetIdentityService_RoleUserAndClaimCrud_Works()
    {
        using var context = CreateContext("AspNetIdentityService_RoleUserAndClaimCrud");
        var service = CreateService(context);

        var role = await service.CreateRoleAsync(new AspNetRole { Id = "r-1", Name = "operators" });
        var fetchedRole = await service.GetRoleByIdAsync(role.Id);
        Assert.NotNull(fetchedRole);
        Assert.Equal("operators", fetchedRole!.Name);

        var byName = await service.GetRoleByNameAsync("operators");
        Assert.NotNull(byName);

        role.Name = "operators-updated";
        var updatedRole = await service.UpdateRoleAsync(role);
        Assert.Equal("operators-updated", updatedRole.Name);

        var roleClaim = await service.CreateRoleClaimAsync(new AspNetRoleClaim
        {
            RoleId = role.Id,
            ClaimType = "permission",
            ClaimValue = "dashboard:read"
        });
        var roleClaims = (await service.GetClaimsByRoleIdAsync(role.Id)).ToList();
        Assert.Single(roleClaims);
        Assert.Equal(roleClaim.ClaimValue, roleClaims[0].ClaimValue);

        var user = await service.CreateUserAsync(new AspNetUser
        {
            Id = "u-1",
            UserName = "carol",
            Email = "carol@example.com",
            PhoneNumber = "555-0300",
            ExternalSubjectId = "carol-subject"
        });

        Assert.NotNull(await service.GetUserByIdAsync(user.Id));
        Assert.NotNull(await service.GetUserByExternalSubjectIdAsync("carol-subject"));
        Assert.NotNull(await service.GetUserByUserNameAsync("carol"));
        Assert.Single((await service.GetAllUsersAsync()).ToList());

        user.Email = "carol+updated@example.com";
        var updatedUser = await service.UpdateUserAsync(user);
        Assert.Equal("carol+updated@example.com", updatedUser.Email);

        var userClaim = await service.CreateUserClaimAsync(new AspNetUserClaim
        {
            UserId = user.Id,
            ClaimType = null,
            ClaimValue = null
        });
        var userClaims = (await service.GetClaimsByUserIdAsync(user.Id)).ToList();
        Assert.Single(userClaims);
        Assert.Equal(userClaim.Id, userClaims[0].Id);

        Assert.Single((await service.GetAllRolesAsync()).ToList());
        Assert.True(await service.DeleteUserAsync(user.Id));
        Assert.True(await service.DeleteRoleAsync(role.Id));
        Assert.Null(await service.GetUserByIdAsync(user.Id));
        Assert.Null(await service.GetRoleByIdAsync(role.Id));
    }

    [Fact]
    public async Task AspNetIdentityService_AssignRole_Branches_AreHandled()
    {
        using var context = CreateContext("AspNetIdentityService_AssignRole_Branches");
        var service = CreateService(context);

        var role = await service.CreateRoleAsync(new AspNetRole { Id = "r-assign", Name = "assign-role" });
        var user = await service.CreateUserAsync(new AspNetUser { Id = "u-assign", UserName = "assign-user", Email = "assign@example.com", PhoneNumber = "555-0400", ExternalSubjectId = "assign-subject" });

        Assert.False(await service.AssignRoleToUserAsync("missing-user", role.Id));
        Assert.False(await service.AssignRoleToUserAsync(user.Id, "missing-role"));

        Assert.True(await service.AssignRoleToUserAsync(user.Id, role.Id));
        Assert.True(await service.AssignRoleToUserAsync(user.Id, role.Id));

        var roleIds = (await service.GetUserRoleIdsAsync(user.Id)).ToList();
        Assert.Single(roleIds);
        Assert.Equal(role.Id, roleIds[0]);
    }

    [Fact]
    public async Task AspNetIdentityService_RemoveRoleAndClaims_Branches_AreHandled()
    {
        using var context = CreateContext("AspNetIdentityService_RemoveRoleAndClaims_Branches");
        var service = CreateService(context);

        var role = await service.CreateRoleAsync(new AspNetRole { Id = "r-remove", Name = "remove-role" });
        var user = await service.CreateUserAsync(new AspNetUser { Id = "u-remove", UserName = "remove-user", Email = "remove@example.com", PhoneNumber = "555-0500", ExternalSubjectId = "remove-subject" });
        await service.CreateRoleClaimAsync(new AspNetRoleClaim { RoleId = role.Id, ClaimType = "permission", ClaimValue = "dashboard:retry" });
        await service.CreateUserClaimAsync(new AspNetUserClaim { UserId = user.Id, ClaimType = null, ClaimValue = null });
        await service.CreateUserClaimAsync(new AspNetUserClaim { UserId = user.Id, ClaimType = "permission", ClaimValue = "dashboard:retry" });
        await service.AssignRoleToUserAsync(user.Id, role.Id);

        Assert.False(await service.RemoveRoleFromUserAsync("missing-user", role.Id));
        Assert.False(await service.RemoveRoleFromUserAsync(user.Id, "missing-role"));
        Assert.True(await service.RemoveRoleFromUserAsync(user.Id, role.Id));

        Assert.Empty((await service.GetUserRoleIdsAsync("missing-user")).ToList());
        Assert.Empty((await service.GetUserRolesAsync("missing-user")).ToList());
        Assert.Empty((await service.GetEffectiveUserClaimsAsync("missing-user")).ToList());

        await service.AssignRoleToUserAsync(user.Id, role.Id);
        var effectiveClaims = (await service.GetEffectiveUserClaimsAsync(user.Id)).ToList();
        Assert.Contains(effectiveClaims, claim => claim.ClaimType == string.Empty && claim.ClaimValue == string.Empty);
        Assert.Contains(effectiveClaims, claim => claim.ClaimType == "permission" && claim.ClaimValue == "dashboard:retry");
        Assert.Single(effectiveClaims.Where(claim => claim.ClaimType == "permission" && claim.ClaimValue == "dashboard:retry"));
    }
}
