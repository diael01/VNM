using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Repositories;
using Services.Inverter;
using Infrastructure.DTOs;
using Repositories.Models;
using Repositories.Models;
using Xunit;
using Infrastructure.DTOs;

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
        var service = new AddressService(repository, null); // Pass null for IMapper if not used in test

        var dto = new AddressDto
        {
            Country = "CountryX",
            County = "CountyY",
            City = "CityZ",
            Street = "Main St",
            StreetNumber = "123",
            PostalCode = "00000",
                // InverterId = 1
        };

        // Simulate mapping manually for test
        var address = new Address
        {
            Country = dto.Country,
            County = dto.County,
            City = dto.City,
            Street = dto.Street,
            StreetNumber = dto.StreetNumber,
            PostalCode = dto.PostalCode,
                // InverterId = dto.InverterId
        };

        var created = await service.CreateAsync(dto);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("CityZ", fetched!.City);

        var updateDto = new AddressDto
        {
            Country = dto.Country,
            County = dto.County,
            City = "NewCity",
            Street = dto.Street,
            StreetNumber = dto.StreetNumber,
            PostalCode = dto.PostalCode,
                // InverterId = dto.InverterId
        };
        var updated = await service.UpdateAsync(created.Id, updateDto);
        Assert.Equal("NewCity", updated.City);

        var all = (await service.GetAllAsync()).ToList();
        Assert.Single(all);

        var deleteResult = await service.DeleteAsync(created.Id);
        Assert.True(deleteResult);

        var notFound = await service.GetByIdAsync(created.Id);
        Assert.Null(notFound);
    }
}