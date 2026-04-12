using Infrastructure.Utils;
using Infrastructure.DTOs;
using System.Net.Http.Json;

namespace Services.Redirect;

public interface IDashboardTransferRuleRedirectService
{
	Task<List<TransferRuleDto>> GetTransferRulesAsync(string accessToken, CancellationToken cancellationToken = default);
	Task<TransferRuleDto?> GetTransferRuleByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default);
	Task<TransferRuleDto> CreateTransferRuleAsync(string accessToken, TransferRuleDto rule, CancellationToken cancellationToken = default);
	Task<TransferRuleDto> UpdateTransferRuleAsync(string accessToken, int id, TransferRuleDto rule, CancellationToken cancellationToken = default);
	Task<bool> DeleteTransferRuleAsync(string accessToken, int id, CancellationToken cancellationToken = default);
}

public sealed class DashboardTransferRuleRedirectService : IDashboardTransferRuleRedirectService
{
	private readonly IHttpClientFactory _httpClientFactory;

	public DashboardTransferRuleRedirectService(IHttpClientFactory httpClientFactory)
	{
		_httpClientFactory = httpClientFactory;
	}

	public async Task<List<TransferRuleDto>> GetTransferRulesAsync(string accessToken, CancellationToken cancellationToken = default)
	{
		var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
		var response = await meterClient.GetAsync("api/v1/TransferRule", cancellationToken);
		if (!response.IsSuccessStatusCode)
			throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

		var rules = await response.Content.ReadFromJsonAsync<List<TransferRuleDto>>(cancellationToken: cancellationToken);
		return rules ?? new List<TransferRuleDto>();
	}

	public async Task<TransferRuleDto?> GetTransferRuleByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default)
	{
		var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
		var response = await meterClient.GetAsync($"api/v1/TransferRule/{id}", cancellationToken);
		if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
		if (!response.IsSuccessStatusCode)
			throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

		return await response.Content.ReadFromJsonAsync<TransferRuleDto>(cancellationToken: cancellationToken);
	}

	public async Task<TransferRuleDto> CreateTransferRuleAsync(string accessToken, TransferRuleDto rule, CancellationToken cancellationToken = default)
	{
		var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
		rule.Id = 0;
		var response = await meterClient.PostAsJsonAsync("api/v1/TransferRule", rule, cancellationToken);
		if (!response.IsSuccessStatusCode)
			throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

		return (await response.Content.ReadFromJsonAsync<TransferRuleDto>(cancellationToken: cancellationToken))!;
	}

	public async Task<TransferRuleDto> UpdateTransferRuleAsync(string accessToken, int id, TransferRuleDto rule, CancellationToken cancellationToken = default)
	{
		var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
		rule.Id = id;
		var response = await meterClient.PutAsJsonAsync($"api/v1/TransferRule/{id}", rule, cancellationToken);
		if (!response.IsSuccessStatusCode)
			throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

		return (await response.Content.ReadFromJsonAsync<TransferRuleDto>(cancellationToken: cancellationToken))!;
	}

	public async Task<bool> DeleteTransferRuleAsync(string accessToken, int id, CancellationToken cancellationToken = default)
	{
		var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
		var response = await meterClient.DeleteAsync($"api/v1/TransferRule/{id}", cancellationToken);
		if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
		if (!response.IsSuccessStatusCode)
			throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");

		return true;
	}
}
