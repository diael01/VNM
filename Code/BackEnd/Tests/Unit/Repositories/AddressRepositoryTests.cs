using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Repositories;
using Repositories.Models;
using Repositories.Models;
using Xunit;

namespace BackEnd.Tests.Unit.Repositories;

public class AddressRepositoryTests
{
    private VnmDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<VnmDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new VnmDbContext(options);
    }

    [Fact]
    public async Task AddAndGetByIdAsync_Works()
    {
        using var context = CreateContext("Address_AddAndGetById");
        var repository = new AddressRepository(context);

        var address = new Address
        {
            Country = "CountryX",
            County = "CountyY",
            City = "CityZ",
            Street = "Main St",
            StreetNumber = "123",
            PostalCode = "00000",
        };

        var created = await repository.AddAsync(address);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        var fetched = await repository.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("CityZ", fetched.City);
    }
}