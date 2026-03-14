using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.CRUD.Repositories;
using Services.Identity;
using Repositories.Models;
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

    [Fact]
    public async Task AspNetIdentityService_SeedDefaults_CreatesRolesUsersAndClaims()
    {
        using var context = CreateContext("AspNetIdentityService_SeedDefaults");
        var roleRepo = new AspNetRoleRepository(context);
        var roleClaimRepo = new AspNetRoleClaimRepository(context);
        var userRepo = new AspNetUserRepository(context);
        var userClaimRepo = new AspNetUserClaimRepository(context);
        var service = new AspNetIdentityService(roleRepo, roleClaimRepo, userRepo, userClaimRepo, context);

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
        var roleRepo = new AspNetRoleRepository(context);
        var roleClaimRepo = new AspNetRoleClaimRepository(context);
        var userRepo = new AspNetUserRepository(context);
        var userClaimRepo = new AspNetUserClaimRepository(context);
        var service = new AspNetIdentityService(roleRepo, roleClaimRepo, userRepo, userClaimRepo, context);

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
}
