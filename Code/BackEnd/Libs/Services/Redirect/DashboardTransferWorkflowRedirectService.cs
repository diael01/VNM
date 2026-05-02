using Infrastructure.DTOs;
using Infrastructure.Utils;
using System.Net.Http.Json;

namespace Services.Redirect;

public interface IDashboardTransferWorkflowRedirectService
{
    Task<List<TransferWorkflowDto>> GetTransferWorkflowsAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<List<TransferWorkflowStatusHistoryDto>> GetTransferWorkflowHistoryAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<TransferWorkflowDto?> GetTransferWorkflowByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default);
    Task<TransferWorkflowDto> ApproveTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default);
    Task<TransferWorkflowDto> RejectTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default);
    Task<TransferWorkflowDto> ExecuteTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default);
    Task<TransferWorkflowDto> SettleTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default);
}

public sealed class DashboardTransferWorkflowRedirectService : IDashboardTransferWorkflowRedirectService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardTransferWorkflowRedirectService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<TransferWorkflowDto>> GetTransferWorkflowsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.GetAsync("api/v1/TransferWorkflow", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        var workflows = await response.Content.ReadFromJsonAsync<List<TransferWorkflowDto>>(cancellationToken: cancellationToken);
        return workflows ?? new List<TransferWorkflowDto>();
    }

    public async Task<List<TransferWorkflowStatusHistoryDto>> GetTransferWorkflowHistoryAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.GetAsync("api/v1/transfers/history", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        var history = await response.Content.ReadFromJsonAsync<List<TransferWorkflowStatusHistoryDto>>(cancellationToken: cancellationToken);
        return history ?? new List<TransferWorkflowStatusHistoryDto>();
    }

    public async Task<TransferWorkflowDto?> GetTransferWorkflowByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.GetAsync($"api/v1/TransferWorkflow/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        return await response.Content.ReadFromJsonAsync<TransferWorkflowDto>(cancellationToken: cancellationToken);
    }

    public async Task<TransferWorkflowDto> ApproveTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.PostAsJsonAsync($"api/v1/TransferWorkflow/{id}/approve", new { note }, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        return (await response.Content.ReadFromJsonAsync<TransferWorkflowDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<TransferWorkflowDto> RejectTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.PostAsJsonAsync($"api/v1/TransferWorkflow/{id}/reject", new { note }, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        return (await response.Content.ReadFromJsonAsync<TransferWorkflowDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<TransferWorkflowDto> ExecuteTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.PostAsJsonAsync($"api/v1/transfer-execution/workflows/{id}/execute", new { note }, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        return (await response.Content.ReadFromJsonAsync<TransferWorkflowDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<TransferWorkflowDto> SettleTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.PostAsJsonAsync($"api/v1/transfer-execution/workflows/{id}/settle", new { note }, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        return (await response.Content.ReadFromJsonAsync<TransferWorkflowDto>(cancellationToken: cancellationToken))!;
    }
}
