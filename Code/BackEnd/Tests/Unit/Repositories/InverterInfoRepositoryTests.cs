using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Repositories;
using Repositories.Data;
using Repositories.Models;
using Xunit;

namespace BackEnd.Tests.Unit.Repositories;

public class InverterInfoRepositoryTests
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
        using var context = CreateContext("InverterInfo_AddAndGetById");
        var repository = new InverterInfoRepository(context);

        var info = new InverterInfo
        {
            InverterType = "TypeA",
            BatteryType = "BatteryB",
            NumberOfSolarPanels = 10,
            SolarPanelType = "PanelC"
        };

        var created = await repository.AddAsync(info);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        var fetched = await repository.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("TypeA", fetched.InverterType);
    }
}