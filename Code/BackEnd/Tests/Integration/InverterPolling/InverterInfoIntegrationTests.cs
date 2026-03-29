using Xunit;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Repositories.Models;

namespace EnergyManagementWeb.IntegrationTests;

public class InverterInfoIntegrationTests : IntegrationTestBase
{
    public InverterInfoIntegrationTests() : base(new CustomWebApplicationFactory()) { }

    [Fact]
    public async Task InverterInfo_CRUD_Works()
    {
        var client = Factory.CreateClient();

        // Create
        var info = new InverterInfo
        {
            Model = "ModelA",
            Manufacturer = "ManuB",
            SerialNumber = "SN123",
            AddressId = 1
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
        Assert.Equal("ModelA", fetched!.Model);
        Assert.Equal("ManuB", fetched!.Manufacturer);
        Assert.Equal("SN123", fetched!.SerialNumber);
        Assert.Equal(1, fetched!.AddressId);

        // Update
        fetched.Model = "ModelX";
        var updateResp = await client.PutAsJsonAsync($"api/v1/inverterinfo/{fetched.Id}", fetched);
        updateResp.EnsureSuccessStatusCode();
        var updated = await updateResp.Content.ReadFromJsonAsync<InverterInfo>();
        Assert.Equal("ModelX", updated!.Model);

        // Delete
        var deleteResp = await client.DeleteAsync($"api/v1/inverterinfo/{fetched.Id}");
        Assert.True(deleteResp.IsSuccessStatusCode);

        // Confirm deleted
        var notFoundResp = await client.GetAsync($"api/v1/inverterinfo/{fetched.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, notFoundResp.StatusCode);
    }
}