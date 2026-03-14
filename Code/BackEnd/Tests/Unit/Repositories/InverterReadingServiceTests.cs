using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Repositories;
using Services.Inverter;
using Repositories.Data;
using Repositories.Models;
using Xunit;

namespace BackEnd.Tests.Unit.Repositories;

public class InverterReadingServiceTests
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
        using var context = CreateContext("InverterReading_Service_CRUD");
        var repository = new InverterReadingRepository(context);
        var service = new InverterReadingService(repository);

        var reading = new InverterReading
        {
            TimestampUtc = DateTime.UtcNow,
            PowerW = 123,
            VoltageV = 345,
            CurrentA = 4,
            Source = "service-test"
        };

        var created = await service.CreateAsync(reading);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("service-test", fetched!.Source);

        created.PowerW = 999;
        var updated = await service.UpdateAsync(created);
        Assert.Equal(999, updated.PowerW);

        var all = (await service.GetAllAsync()).ToList();
        Assert.Single(all);

        var deleteResult = await service.DeleteAsync(created.Id);
        Assert.True(deleteResult);

        var notFound = await service.GetByIdAsync(created.Id);
        Assert.Null(notFound);
    }
}
