using System.Net.Http.Json;
using Repositories.Models;
using Infrastructure.Utils;

namespace Services.Redirect;

public interface IDashboardDailyBalanceRedirectService
{
	Task<List<DailyEnergyBalance>> GetDailyBalanceAsync(string accessToken, CancellationToken cancellationToken = default);
}

public sealed class DashboardDailyBalanceRedirectService : IDashboardDailyBalanceRedirectService
{
	private readonly IHttpClientFactory _httpClientFactory;

	public DashboardDailyBalanceRedirectService(IHttpClientFactory httpClientFactory)
	{
		_httpClientFactory = httpClientFactory;
	}

	public async Task<List<DailyEnergyBalance>> GetDailyBalanceAsync(
		string accessToken,
		CancellationToken cancellationToken = default)
	{
		var meterClient = EnergyManagementApiClientHelper.CreateAuthorizedMeterClient(_httpClientFactory, accessToken);
		var response = await meterClient.GetAsync("api/v1/Analytics/DailyBalance", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"EnergyManagement API returned status code {(int)response.StatusCode}.");
		}
		var balances = await response.Content.ReadFromJsonAsync<List<DailyEnergyBalance>>(cancellationToken: cancellationToken);
		return balances ?? new List<DailyEnergyBalance>();
	}
}
