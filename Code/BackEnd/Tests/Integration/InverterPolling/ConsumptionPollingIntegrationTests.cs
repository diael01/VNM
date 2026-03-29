using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Repositories.Models;

namespace EnergyManagementWeb.IntegrationTests;

[Collection("IntegrationTests")]
public class ConsumptionPollingIntegrationTests : IntegrationTestBase
{
    public ConsumptionPollingIntegrationTests() : base(new CustomWebApplicationFactory()) {}

    [Fact]
    public async Task PollingService_Should_SaveReadingToDatabase()
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<VnmDbContext>>();

        // Wait for background polling to complete
        await Task.Delay(500);

        await using var db = await dbFactory.CreateDbContextAsync();
        var readings = await db.ConsumptionReadings.ToListAsync();

        Assert.Single(readings);
        Assert.Equal(100, readings[0].Power); // Adjust as needed for your simulator
        Assert.Equal("Provider", readings[0].Source); // Adjust as needed for your config
    }
}
