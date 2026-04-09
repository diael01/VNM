using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Repositories;
using Repositories.Models;
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
        var repositoryUnderTest = repository;

        var reading = new InverterReading
        {
            Timestamp = DateTime.UtcNow,
            Power = 123,
            Voltage = 345,
            Current = 4,
            Source = "service-test"
        };

        var created = await repositoryUnderTest.AddAsync(reading);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        var fetched = await repositoryUnderTest.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("service-test", fetched!.Source);

        created.Power = 999;
        var updated = await repositoryUnderTest.UpdateAsync(created);
        Assert.Equal(999, updated.Power);

        var all = (await repositoryUnderTest.GetAllAsync()).ToList();
        Assert.Single(all);

        var deleteResult = await repositoryUnderTest.DeleteAsync(created.Id);
        Assert.True(deleteResult);

        var notFound = await repositoryUnderTest.GetByIdAsync(created.Id);
        Assert.Null(notFound);
    }
}
