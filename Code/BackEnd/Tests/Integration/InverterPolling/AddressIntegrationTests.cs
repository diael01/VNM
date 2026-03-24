using Xunit;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Services.DTOs;
using Infrastructure.DTOs;

namespace MeterIngestionWeb.IntegrationTests;

public class AddressIntegrationTests : IntegrationTestBase
{
    public AddressIntegrationTests() : base(new CustomWebApplicationFactory()) { }

    [Fact]
    public async Task Address_CRUD_Works()
    {
        var client = Factory.CreateClient();

        // Create
        var address = new AddressDto
        {
            Country = "CountryX",
            County = "CountyY",
            City = "CityZ",
            Street = "Main St",
            StreetNumber = "123",
            PostalCode = "00000",
            InverterId = 1
        };
        var createResp = await client.PostAsJsonAsync("api/v1/address", address);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(created);

        // GetById
        var getResp = await client.GetAsync($"api/v1/address/{1}");
        getResp.EnsureSuccessStatusCode();
        var fetched = await getResp.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(fetched);
        Assert.Equal("CityZ", fetched!.City);

        // Update
        fetched.City = "NewCity";
        var updateResp = await client.PutAsJsonAsync($"api/v1/address/{1}", fetched);
        updateResp.EnsureSuccessStatusCode();
        var updated = await updateResp.Content.ReadFromJsonAsync<AddressDto>();
        Assert.Equal("NewCity", updated!.City);

        // Delete
        var deleteResp = await client.DeleteAsync($"api/v1/address/{1}");
        Assert.True(deleteResp.IsSuccessStatusCode);

        // Confirm deleted
        var notFoundResp = await client.GetAsync($"api/v1/address/{1}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, notFoundResp.StatusCode);
    }
}