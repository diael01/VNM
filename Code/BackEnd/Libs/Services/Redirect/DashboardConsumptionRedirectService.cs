using System.Net.Http.Json;
using Repositories.Models;
using Infrastructure.Utils;
namespace Services.Redirect;

public interface IDashboardConsumptionRedirectService
{
    Task<List<ConsumptionReading>> GetAllConsumptionReadingsAsync(string accessToken, CancellationToken cancellationToken = default);
     Task<ConsumptionReading?> GetConsumptionReadingByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default);
    Task<ConsumptionReading> CreateConsumptionReadingAsync(string accessToken, ConsumptionReading reading, CancellationToken cancellationToken = default);
    Task<ConsumptionReading> UpdateConsumptionReadingAsync(string accessToken, int id, ConsumptionReading reading, CancellationToken cancellationToken = default);
    Task<bool> DeleteConsumptionReadingAsync(string accessToken, int id, CancellationToken cancellationToken = default);
 }
 
public sealed class DashboardConsumptionRedirectService : IDashboardConsumptionRedirectService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardConsumptionRedirectService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<ConsumptionReading>> GetAllConsumptionReadingsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var client = MeterApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.GetAsync("api/v1/ConsumptionReadings", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");
        var readings = await response.Content.ReadFromJsonAsync<List<ConsumptionReading>>(cancellationToken: cancellationToken);
        return readings ?? new List<ConsumptionReading>();
    }

    public async Task<ConsumptionReading?> GetConsumptionReadingByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default)
    {
        var client = MeterApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.GetAsync($"api/v1/consumptionreadings/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");
        return await response.Content.ReadFromJsonAsync<ConsumptionReading>(cancellationToken: cancellationToken);
    }

    public async Task<ConsumptionReading> CreateConsumptionReadingAsync(string accessToken, ConsumptionReading reading, CancellationToken cancellationToken = default)
    {
        var client = MeterApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        reading.Id = 0;
        var response = await client.PostAsJsonAsync("api/v1/consumptionreadings", reading, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<ConsumptionReading>(cancellationToken: cancellationToken))!;
    }

    public async Task<ConsumptionReading> UpdateConsumptionReadingAsync(string accessToken, int id, ConsumptionReading reading, CancellationToken cancellationToken = default)
    {
        var client = MeterApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        reading.Id = id;
        var response = await client.PutAsJsonAsync($"api/v1/consumptionreadings/{id}", reading, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<ConsumptionReading>(cancellationToken: cancellationToken))!;
    }

    public async Task<bool> DeleteConsumptionReadingAsync(string accessToken, int id, CancellationToken cancellationToken = default)
    {
        var client = MeterApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.DeleteAsync($"api/v1/consumptionreadings/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");
        return true;
    } 
}
