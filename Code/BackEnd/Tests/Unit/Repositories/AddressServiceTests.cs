using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Repositories;
using Services.Inverter;
using Repositories.Data;
using Repositories.Models;
using Xunit;

namespace BackEnd.Tests.Unit.Repositories;

public class AddressServiceTests
{
    private VnmDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<VnmDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new VnmDbContext(options);
    }

    [Fact]
    public async Task Service_CRUD_Works()
    {
        using var context = CreateContext("Address_Service_CRUD");
        var repository = new AddressRepository(context);
        var service = new AddressService(repository);

        var address = new Address
        {
            Country = "CountryX",
            County = "CountyY",
            City = "CityZ",
            Street = "Main St",
            StreetNumber = "123",
            PostalCode = "00000",
            InverterInfoId = 1
        };

        var created = await service.CreateAsync(address);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("CityZ", fetched!.City);

        created.City = "NewCity";
        var updated = await service.UpdateAsync(created);
        Assert.Equal("NewCity", updated.City);

        var all = (await service.GetAllAsync()).ToList();
        Assert.Single(all);

        var deleteResult = await service.DeleteAsync(created.Id);
        Assert.True(deleteResult);

        var notFound = await service.GetByIdAsync(created.Id);
        Assert.Null(notFound);
    }
}