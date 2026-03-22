using Xunit;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Repositories.Models;

namespace MeterIngestionWeb.IntegrationTests;

[Collection("IntegrationTests")]
public class InverterInfoIntegrationTests : IntegrationTestBase
{
    public InverterInfoIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task InverterInfo_CRUD_Works()
    {
        var client = Factory.CreateClient();

        // Create
        var info = new InverterInfo
        {
            InverterType = "TypeA",
            BatteryType = "BatteryB",
            NumberOfSolarPanels = 10,
            SolarPanelType = "PanelC"
        };
        var createResp = await client.PostAsJsonAsync("api/v1/inverterinfo", info);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<InverterInfo>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        // GetById
        var getResp = await client.GetAsync($"api/v1/inverterinfo/{created.Id}");
        getResp.EnsureSuccessStatusCode();
        var fetched = await getResp.Content.ReadFromJsonAsync<InverterInfo>();
        Assert.NotNull(fetched);
        Assert.Equal("TypeA", fetched!.InverterType);

        // Update
        fetched.BatteryType = "BatteryX";
        var updateResp = await client.PutAsJsonAsync($"api/v1/inverterinfo/{fetched.Id}", fetched);
        updateResp.EnsureSuccessStatusCode();
        var updated = await updateResp.Content.ReadFromJsonAsync<InverterInfo>();
        Assert.Equal("BatteryX", updated!.BatteryType);

        // Delete
        var deleteResp = await client.DeleteAsync($"api/v1/inverterinfo/{fetched.Id}");
        Assert.True(deleteResp.IsSuccessStatusCode);

        // Confirm deleted
        var notFoundResp = await client.GetAsync($"api/v1/inverterinfo/{fetched.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, notFoundResp.StatusCode);
    }
}