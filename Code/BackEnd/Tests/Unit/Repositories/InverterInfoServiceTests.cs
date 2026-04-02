using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Repositories;
using Services.Inverter;
using Repositories.Models;
using Repositories.Models;
using Xunit;
using Infrastructure.DTOs;
using AutoMapper;

namespace BackEnd.Tests.Unit.Repositories;

public class InverterInfoServiceTests
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
        using var context = CreateContext("InverterInfo_Service_CRUD");
        var repository = new InverterInfoRepository(context);
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
        var service = new InverterInfoService(repository, mapper);

        var info = new InverterInfoDto
        {
            Model = "ModelX",
            Manufacturer = "BrandY",
            SerialNumber = "SN123",
            AddressId = 1
        };

        var created = await service.CreateAsync(info);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("ModelX", fetched!.Model);

        created.Manufacturer = "BrandZ";
        var updated = await service.UpdateAsync(created.Id, created);
        Assert.Equal("BrandZ", updated.Manufacturer);

        var all = (await service.GetAllAsync()).ToList();
        Assert.Single(all);

        var deleteResult = await service.DeleteAsync(created.Id);
        Assert.True(deleteResult);

        var notFound = await service.GetByIdAsync(created.Id);
        Assert.Null(notFound);
    }
}