using System.Net.Http.Json;
using Infrastructure.DTOs;
using Xunit;

namespace EnergyManagementWeb.IntegrationTests;

public class TransferRuleControllerIntegrationTests : IntegrationTestBase
{
    public TransferRuleControllerIntegrationTests() : base(new CustomWebApplicationFactory()) { }

    [Fact]
    public async Task TransferRuleController_CreateAndGetById_WithNullOptionalFields_Works()
    {
        var client = Factory.CreateClient();

        var sourceAddress = new AddressDto
        {
            Country = "CountryA",
            County = "CountyA",
            City = "SourceCity",
            Street = "Source Street",
            StreetNumber = "1",
            PostalCode = "1000",
            InverterId = 1,
        };

        var destinationAddress = new AddressDto
        {
            Country = "CountryA",
            County = "CountyA",
            City = "DestCity",
            Street = "Dest Street",
            StreetNumber = "2",
            PostalCode = "1001",
            InverterId = 2,
        };

        var sourceCreate = await client.PostAsJsonAsync("api/v1/address", sourceAddress);
        sourceCreate.EnsureSuccessStatusCode();
        var source = await sourceCreate.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(source);

        var destinationCreate = await client.PostAsJsonAsync("api/v1/address", destinationAddress);
        destinationCreate.EnsureSuccessStatusCode();
        var destination = await destinationCreate.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(destination);

        var createRequest = new TransferRuleDto
        {
            Id = 0,
            SourceAddressId = source!.Id,
            DestinationAddressId = destination!.Id,
            IsEnabled = true,
            Priority = 1,
            DistributionMode = 0,
            MaxDailyKwh = null,
            WeightPercent = null,
        };

        var createResp = await client.PostAsJsonAsync("api/v1/TransferRule", createRequest);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<TransferRuleDto>();

        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Null(created.MaxDailyKwh);
        Assert.Null(created.WeightPercent);

        var getResp = await client.GetAsync($"api/v1/TransferRule/{created.Id}");
        getResp.EnsureSuccessStatusCode();
        var fetched = await getResp.Content.ReadFromJsonAsync<TransferRuleDto>();

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(createRequest.SourceAddressId, fetched.SourceAddressId);
        Assert.Equal(createRequest.DestinationAddressId, fetched.DestinationAddressId);
        Assert.Equal(createRequest.DistributionMode, fetched.DistributionMode);
    }
}
