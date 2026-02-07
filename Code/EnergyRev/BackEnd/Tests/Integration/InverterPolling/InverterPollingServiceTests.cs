using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using InverterPolling.Data;
using System.Threading.Tasks;

namespace MeterIngestionService.IntegrationTests;

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory> { }

[Collection("IntegrationTests")]
public class InverterPollingIntegrationTests : IntegrationTestBase
{
    public InverterPollingIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory) { }

    [Fact]
    public async Task PollingService_Should_SaveReadingToDatabase()
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SolarDbContext>>();

        // Wait for background polling to complete
        await Task.Delay(500);

        await using var db = await dbFactory.CreateDbContextAsync();
        var readings = await db.InverterReadings.ToListAsync();

        Assert.Single(readings);
        Assert.Equal(100, readings[0].PowerW);
        Assert.Equal("Test", readings[0].Source);
    }

}
