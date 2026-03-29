using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Repositories;
using Repositories.Models;
using Repositories.Models;
using Xunit;

namespace BackEnd.Tests.Unit.Repositories;

public class InverterReadingRepositoryTests
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
        using var context = CreateContext("InverterReading_AddAndGetById");
        var repository = new InverterReadingRepository(context);

        var reading = new InverterReading
        {
            Timestamp = DateTime.UtcNow,
            Power = 100,
            Voltage = 230,
            Current = 1,
            Source = "unit-test"
        };

        var created = await repository.AddAsync(reading);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        var fetched = await repository.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("unit-test", fetched.Source);
    }

    [Fact]
    public async Task GetLatestReadingsAsync_ReturnsOrdered_Readings()
    {
        using var context = CreateContext("InverterReading_GetLatestReadings");
        var repository = new InverterReadingRepository(context);

        var r1 = new InverterReading { Timestamp = DateTime.UtcNow.AddMinutes(-10), Power = 10, Voltage = 200, Current = 1, Source = "t1" };
        var r2 = new InverterReading { Timestamp = DateTime.UtcNow.AddMinutes(-5), Power = 20, Voltage = 210, Current = 1, Source = "t2" };
        var r3 = new InverterReading { Timestamp = DateTime.UtcNow, Power = 30, Voltage = 220, Current = 2, Source = "t3" };

        await repository.AddAsync(r1);
        await repository.AddAsync(r2);
        await repository.AddAsync(r3);

        var latest = (await repository.GetLatestReadingsAsync(2)).ToList();

        Assert.Equal(2, latest.Count);
        Assert.Equal("t3", latest[0].Source);
        Assert.Equal("t2", latest[1].Source);
    }
}
