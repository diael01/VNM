using Microsoft.EntityFrameworkCore;
using Repositories.Models;
using Repositories.CRUD.Repositories;
using Repositories.Models;
using Xunit;

namespace BackEnd.Tests.Unit.Repositories;

public class AspNetIdentityRepositoryTests
{
    private VnmDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<VnmDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new VnmDbContext(options);
    }

    [Fact]
    public async Task RoleCrud_And_GetByName_Works()
    {
        using var context = CreateContext("AspNetRoleCrud");
        var roleRepo = new AspNetRoleRepository(context);

        var role = new AspNetRole { Id = "role-admin", Name = "admin" };
        var created = await roleRepo.AddAsync(role);

        Assert.NotNull(created);
        Assert.Equal("admin", created.Name);

        var loadedById = await roleRepo.GetByIdAsync("role-admin");
        Assert.NotNull(loadedById);
        Assert.Equal("admin", loadedById!.Name);

        var loadedByName = await roleRepo.GetByNameAsync("admin");
        Assert.NotNull(loadedByName);
        Assert.Equal("role-admin", loadedByName!.Id);

        role.Name = "administrator";
        var updated = await roleRepo.UpdateAsync(role);
        Assert.Equal("administrator", updated.Name);

        var deleted = await roleRepo.DeleteAsync("role-admin");
        Assert.True(deleted);
    }

    [Fact]
    public async Task RoleClaim_Crud_And_GetForRole_Works()
    {
        using var context = CreateContext("AspNetRoleClaimCrud");
        var roleRepo = new AspNetRoleRepository(context);
        var claimRepo = new AspNetRoleClaimRepository(context);

        var role = await roleRepo.AddAsync(new AspNetRole { Id = "r1", Name = "contributors" });
        var claim = await claimRepo.AddAsync(new AspNetRoleClaim { RoleId = role.Id, ClaimType = "permission", ClaimValue = "dashboard:read" });

        Assert.NotNull(claim);

        var claims = (await claimRepo.GetForRoleAsync(role.Id)).ToList();
        Assert.Single(claims);
        Assert.Equal("dashboard:read", claims[0].ClaimValue);
    }

    [Fact]
    public async Task User_Crud_And_FindByUserName_Works()
    {
        using var context = CreateContext("AspNetUserCrud");
        var userRepo = new AspNetUserRepository(context);

        var user = new AspNetUser { Id = "u1", UserName = "alice", Email = "alice@example.com", PhoneNumber = "555-0100", ExternalSubjectId = "alice-subject" };
        var created = await userRepo.AddAsync(user);

        Assert.NotNull(created);
        Assert.Equal("alice", created.UserName);

        var byId = await userRepo.GetByIdAsync("u1");
        Assert.NotNull(byId);

        var byName = await userRepo.GetByUserNameAsync("alice");
        Assert.NotNull(byName);
        Assert.Equal("u1", byName!.Id);

        created.Email = "alice2@example.com";
        var updated = await userRepo.UpdateAsync(created);
        Assert.Equal("alice2@example.com", updated.Email);

        var deleted = await userRepo.DeleteAsync("u1");
        Assert.True(deleted);
    }

    [Fact]
    public async Task UserClaim_Crud_And_GetForUser_Works()
    {
        using var context = CreateContext("AspNetUserClaimCrud");
        var userRepo = new AspNetUserRepository(context);
        var userClaimRepo = new AspNetUserClaimRepository(context);

        var user = await userRepo.AddAsync(new AspNetUser { Id = "u2", UserName = "bob", Email = "bob@example.com", PhoneNumber = "555-0200", ExternalSubjectId = "bob-subject" });
        await userClaimRepo.AddAsync(new AspNetUserClaim { UserId = user.Id, ClaimType = "permission", ClaimValue = "dashboard:read" });

        var claims = (await userClaimRepo.GetForUserAsync(user.Id)).ToList();
        Assert.Single(claims);
        Assert.Equal("dashboard:read", claims[0].ClaimValue);
    }
}
