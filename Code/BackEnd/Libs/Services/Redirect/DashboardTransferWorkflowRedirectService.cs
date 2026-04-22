using Infrastructure.DTOs;
using Infrastructure.Utils;
using System.Net.Http.Json;

namespace Services.Redirect;

public interface IDashboardTransferWorkflowRedirectService
{
    Task<List<TransferWorkflowDto>> GetTransferWorkflowsAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<TransferWorkflowDto?> GetTransferWorkflowByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default);
    Task<TransferWorkflowDto> CreateTransferWorkflowAsync(string accessToken, TransferWorkflowDto workflow, CancellationToken cancellationToken = default);
    Task<TransferWorkflowDto> UpdateTransferWorkflowAsync(string accessToken, int id, TransferWorkflowDto workflow, CancellationToken cancellationToken = default);
    Task<TransferWorkflowDto> ApproveTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default);
    Task<TransferWorkflowDto> RejectTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default);
    Task<TransferWorkflowDto> ExecuteTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default);
    Task<TransferWorkflowDto> SettleTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteTransferWorkflowAsync(string accessToken, int id, CancellationToken cancellationToken = default);
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

    public async Task<TransferWorkflowDto?> GetTransferWorkflowByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.GetAsync($"api/v1/TransferWorkflow/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        return await response.Content.ReadFromJsonAsync<TransferWorkflowDto>(cancellationToken: cancellationToken);
    }

    public async Task<TransferWorkflowDto> CreateTransferWorkflowAsync(string accessToken, TransferWorkflowDto workflow, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        workflow.Id = 0;
        var response = await meterClient.PostAsJsonAsync("api/v1/TransferWorkflow", workflow, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        return (await response.Content.ReadFromJsonAsync<TransferWorkflowDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<TransferWorkflowDto> UpdateTransferWorkflowAsync(string accessToken, int id, TransferWorkflowDto workflow, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        workflow.Id = id;
        var response = await meterClient.PutAsJsonAsync($"api/v1/TransferWorkflow/{id}", workflow, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        return (await response.Content.ReadFromJsonAsync<TransferWorkflowDto>(cancellationToken: cancellationToken))!;
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
        var response = await meterClient.PostAsJsonAsync($"api/v1/TransferWorkflow/{id}/execute", new { note }, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        return (await response.Content.ReadFromJsonAsync<TransferWorkflowDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<TransferWorkflowDto> SettleTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.PostAsJsonAsync($"api/v1/TransferWorkflow/{id}/settle", new { note }, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        return (await response.Content.ReadFromJsonAsync<TransferWorkflowDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<bool> DeleteTransferWorkflowAsync(string accessToken, int id, CancellationToken cancellationToken = default)
    {
        var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
        var response = await meterClient.DeleteAsync($"api/v1/TransferWorkflow/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

        return true;
    }
}
