
using Microsoft.EntityFrameworkCore;
using Repositories.Models;
using Xunit;

namespace BackEnd.Tests.Unit.Repositories;

public class ConsumptionReadingServiceTests
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
        using var context = CreateContext("ConsumptionReading_Service_CRUD");
        var repository = new FakeConsumptionReadingRepository(context);
        var service = new FakeConsumptionReadingService(repository);

        var reading = new ConsumptionReading
        {
            Timestamp = DateTime.UtcNow,
            Power = 123,
            Source = "service-test"
        };

        var created = await service.CreateAsync(reading);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("service-test", fetched!.Source);

        created.Power = 999;
        var updated = await service.UpdateAsync(created);
        Assert.Equal(999, updated.Power);

        var all = (await service.GetAllAsync()).ToList();
        Assert.Single(all);

        var deleteResult = await service.DeleteAsync(created.Id);
        Assert.True(deleteResult);

        var notFound = await service.GetByIdAsync(created.Id);
        Assert.Null(notFound);
    }
}