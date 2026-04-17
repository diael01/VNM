using Infrastructure.DTOs;
using Infrastructure.Utils;
using System.Net.Http.Json;

namespace Services.Redirect;

public interface IDashboardSourceTransferPolicyRedirectService
{
    Task<List<SourceTransferPolicyDto>> GetPoliciesAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<SourceTransferPolicyDto?> GetPolicyByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default);
    Task<SourceTransferPolicyDto> CreatePolicyAsync(string accessToken, SourceTransferPolicyDto dto, CancellationToken cancellationToken = default);
    Task<SourceTransferPolicyDto> UpdatePolicyAsync(string accessToken, int id, SourceTransferPolicyDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeletePolicyAsync(string accessToken, int id, CancellationToken cancellationToken = default);
    Task<List<TransferRuleDto>> GetRulesAsync(string accessToken, int policyId, CancellationToken cancellationToken = default);
    Task<List<SourceTransferScheduleDto>> GetSchedulesAsync(string accessToken, int policyId, CancellationToken cancellationToken = default);
    Task<SourceTransferScheduleDto> CreateScheduleAsync(string accessToken, int policyId, SourceTransferScheduleDto dto, CancellationToken cancellationToken = default);
    Task<SourceTransferScheduleDto> UpdateScheduleAsync(string accessToken, int policyId, int scheduleId, SourceTransferScheduleDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteScheduleAsync(string accessToken, int policyId, int scheduleId, CancellationToken cancellationToken = default);
}

public sealed class DashboardSourceTransferPolicyRedirectService : IDashboardSourceTransferPolicyRedirectService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardSourceTransferPolicyRedirectService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private static string Base => "api/v1/SourceTransferPolicy";

    public async Task<List<SourceTransferPolicyDto>> GetPoliciesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var client = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.GetAsync(Base, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<List<SourceTransferPolicyDto>>(cancellationToken: cancellationToken)) ?? new();
    }

    public async Task<SourceTransferPolicyDto?> GetPolicyByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default)
    {
        var client = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.GetAsync($"{Base}/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned {(int)response.StatusCode}.");
        return await response.Content.ReadFromJsonAsync<SourceTransferPolicyDto>(cancellationToken: cancellationToken);
    }

    public async Task<SourceTransferPolicyDto> CreatePolicyAsync(string accessToken, SourceTransferPolicyDto dto, CancellationToken cancellationToken = default)
    {
        dto.Id = 0;
        var client = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.PostAsJsonAsync(Base, dto, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<SourceTransferPolicyDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<SourceTransferPolicyDto> UpdatePolicyAsync(string accessToken, int id, SourceTransferPolicyDto dto, CancellationToken cancellationToken = default)
    {
        dto.Id = id;
        var client = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.PutAsJsonAsync($"{Base}/{id}", dto, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<SourceTransferPolicyDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<bool> DeletePolicyAsync(string accessToken, int id, CancellationToken cancellationToken = default)
    {
        var client = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.DeleteAsync($"{Base}/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned {(int)response.StatusCode}.");
        return true;
    }

    public async Task<List<TransferRuleDto>> GetRulesAsync(string accessToken, int policyId, CancellationToken cancellationToken = default)
    {
        var client = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.GetAsync($"{Base}/{policyId}/rules", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<List<TransferRuleDto>>(cancellationToken: cancellationToken)) ?? new();
    }

    public async Task<List<SourceTransferScheduleDto>> GetSchedulesAsync(string accessToken, int policyId, CancellationToken cancellationToken = default)
    {
        var client = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.GetAsync($"{Base}/{policyId}/schedules", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<List<SourceTransferScheduleDto>>(cancellationToken: cancellationToken)) ?? new();
    }

    public async Task<SourceTransferScheduleDto> CreateScheduleAsync(string accessToken, int policyId, SourceTransferScheduleDto dto, CancellationToken cancellationToken = default)
    {
        dto.Id = 0;
        dto.SourceTransferPolicyId = policyId;
        var client = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.PostAsJsonAsync($"{Base}/{policyId}/schedules", dto, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<SourceTransferScheduleDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<SourceTransferScheduleDto> UpdateScheduleAsync(string accessToken, int policyId, int scheduleId, SourceTransferScheduleDto dto, CancellationToken cancellationToken = default)
    {
        dto.SourceTransferPolicyId = policyId;
        var client = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.PutAsJsonAsync($"{Base}/{policyId}/schedules/{scheduleId}", dto, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned {(int)response.StatusCode}.");
        return (await response.Content.ReadFromJsonAsync<SourceTransferScheduleDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<bool> DeleteScheduleAsync(string accessToken, int policyId, int scheduleId, CancellationToken cancellationToken = default)
    {
        var client = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await client.DeleteAsync($"{Base}/{policyId}/schedules/{scheduleId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned {(int)response.StatusCode}.");
        return true;
    }
}
