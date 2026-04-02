using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Utils;
using Infrastructure.DTOs;
using Repositories.Models;

namespace Services.Redirect;

public interface IDashboardInverterRedirectService
{
    Task<List<InverterReading>> GetInverterReadingsAsync(string accessToken, CancellationToken cancellationToken = default);

    // InverterInfo CRUD (DTO-based)
    Task<List<InverterInfoDto>> GetAllInverterInfoAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<InverterInfoDto?> GetInverterInfoByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default);
    Task<InverterInfoDto> CreateInverterInfoAsync(string accessToken, InverterInfoDto info, CancellationToken cancellationToken = default);
    Task<InverterInfoDto> UpdateInverterInfoAsync(string accessToken, int id, InverterInfoDto info, CancellationToken cancellationToken = default);
    Task<bool> DeleteInverterInfoAsync(string accessToken, int id, CancellationToken cancellationToken = default);
}

public sealed class DashboardInverterRedirectService : IDashboardInverterRedirectService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardInverterRedirectService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<InverterReading>> GetInverterReadingsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var meterClient = MeterApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);

        var response = await meterClient.GetAsync("api/v1/InverterReadings", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"EnergyManagement API returned status code {(int)response.StatusCode}.");
        }

        var readings = await response.Content.ReadFromJsonAsync<List<InverterReading>>(cancellationToken: cancellationToken);
        return readings ?? new List<InverterReading>();
    }
    public async Task<List<InverterInfoDto>> GetAllInverterInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var meterClient = MeterApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.GetAsync("api/v1/InverterInfo", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");
        var infos = await response.Content.ReadFromJsonAsync<List<InverterInfoDto>>(cancellationToken: cancellationToken);
        return infos ?? new List<InverterInfoDto>();
    }

    public async Task<InverterInfoDto?> GetInverterInfoByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default)
    {
        var meterClient = MeterApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.GetAsync($"api/v1/InverterInfo/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"MeterIngestion API returned status code {(int)response.StatusCode}.");
        return await response.Content.ReadFromJsonAsync<InverterInfoDto>(cancellationToken: cancellationToken);
    }

    public async Task<InverterInfoDto> CreateInverterInfoAsync(string accessToken, InverterInfoDto info, CancellationToken cancellationToken = default)
    {
        var meterClient = MeterApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        info.Id = 0;
        var response = await meterClient.PostAsJsonAsync("api/v1/InverterInfo", info, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"MeterIngestion API returned status code {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<InverterInfoDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<InverterInfoDto> UpdateInverterInfoAsync(string accessToken, int id, InverterInfoDto info, CancellationToken cancellationToken = default)
    {
        var meterClient = MeterApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        info.Id = id;
        var response = await meterClient.PutAsJsonAsync($"api/v1/InverterInfo/{id}", info, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"MeterIngestion API returned status code {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<InverterInfoDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<bool> DeleteInverterInfoAsync(string accessToken, int id, CancellationToken cancellationToken = default)
    {
        var meterClient = MeterApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.DeleteAsync($"api/v1/InverterInfo/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"MeterIngestion API returned status code {(int)response.StatusCode}.");
        return true;
    }
}
