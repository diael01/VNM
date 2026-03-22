using Xunit;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Repositories.Models;

namespace MeterIngestionWeb.IntegrationTests;

[Collection("IntegrationTests")]
public class AddressIntegrationTests : IntegrationTestBase
{
    public AddressIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Address_CRUD_Works()
    {
        var client = Factory.CreateClient();

        // Create
        var address = new Address
        {
            Country = "CountryX",
            County = "CountyY",
            City = "CityZ",
            Street = "Main St",
            StreetNumber = "123",
            PostalCode = "00000",
            InverterInfoId = 1
        };
        var createResp = await client.PostAsJsonAsync("api/v1/address", address);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<Address>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        // GetById
        var getResp = await client.GetAsync($"api/v1/address/{created.Id}");
        getResp.EnsureSuccessStatusCode();
        var fetched = await getResp.Content.ReadFromJsonAsync<Address>();
        Assert.NotNull(fetched);
        Assert.Equal("CityZ", fetched!.City);

        // Update
        fetched.City = "NewCity";
        var updateResp = await client.PutAsJsonAsync($"api/v1/address/{fetched.Id}", fetched);
        updateResp.EnsureSuccessStatusCode();
        var updated = await updateResp.Content.ReadFromJsonAsync<Address>();
        Assert.Equal("NewCity", updated!.City);

        // Delete
        var deleteResp = await client.DeleteAsync($"api/v1/address/{fetched.Id}");
        Assert.True(deleteResp.IsSuccessStatusCode);

        // Confirm deleted
        var notFoundResp = await client.GetAsync($"api/v1/address/{fetched.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, notFoundResp.StatusCode);
    }
}