using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;
using Models.Dashboard;

namespace Services.Redirect;

public sealed class DashboardRedirectService : IDashboardRedirectService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardRedirectService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<DashboardResponseDto> GetDashboardAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Access token exists: " + !string.IsNullOrWhiteSpace(accessToken));

        var inverterClient = _httpClientFactory.CreateClient("inverter-api");
        inverterClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var inverterResponse = await inverterClient.GetAsync("api/v1/inverter/data", cancellationToken);

        if (!inverterResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Inverter API returned status code {(int)inverterResponse.StatusCode}.");
        }

        var inverterData = await inverterResponse.Content.ReadFromJsonAsync<InverterDataDto>(cancellationToken: cancellationToken);

        return new DashboardResponseDto
        {
            Inverter = inverterData
        };
    }
}