using AutoMapper;
using Infrastructure.DTOs;
using Repositories.Models;
using System.Net.Http.Json;
using Infrastructure.Utils;

namespace Services.Redirect;

public interface IDashboardAddressRedirectService
{
    Task<List<Address>> GetAddressesAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<Address?> GetAddressByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default);
    Task<Address> CreateAddressAsync(string accessToken, AddressDto addressDto, CancellationToken cancellationToken = default);
    Task<Address> UpdateAddressAsync(string accessToken, int id, AddressDto addressDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAddressAsync(string accessToken, int id, CancellationToken cancellationToken = default);
}

public sealed class DashboardAddressRedirectService : IDashboardAddressRedirectService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMapper _mapper;

    public DashboardAddressRedirectService(IHttpClientFactory httpClientFactory, IMapper mapper)
    {
        _httpClientFactory = httpClientFactory;
        _mapper = mapper;
    }

    public async Task<List<Address>> GetAddressesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.GetAsync("api/v1/address", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");
        var addresses = await response.Content.ReadFromJsonAsync<List<Address>>(cancellationToken: cancellationToken);
        return addresses ?? new List<Address>();
    }

    public async Task<Address?> GetAddressByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.GetAsync($"api/v1/address/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"MeterIngestion API returned status code {(int)response.StatusCode}.");
        return await response.Content.ReadFromJsonAsync<Address>(cancellationToken: cancellationToken);
    }

    public async Task<Address> CreateAddressAsync(string accessToken, AddressDto addressDto, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var address = _mapper.Map<Address>(addressDto);
        address.Id = 0;
        var response = await meterClient.PostAsJsonAsync("api/v1/address", address, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"MeterIngestion API returned status code {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<Address>(cancellationToken: cancellationToken))!;
    }

    public async Task<Address> UpdateAddressAsync(string accessToken, int id, AddressDto addressDto, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var address = _mapper.Map<Address>(addressDto);
        address.Id = id;
        var response = await meterClient.PutAsJsonAsync($"api/v1/address/{id}", address, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"MeterIngestion API returned status code {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<Address>(cancellationToken: cancellationToken))!;
    }

    public async Task<bool> DeleteAddressAsync(string accessToken, int id, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.DeleteAsync($"api/v1/address/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"MeterIngestion API returned status code {(int)response.StatusCode}.");
        return true;
    }
}
