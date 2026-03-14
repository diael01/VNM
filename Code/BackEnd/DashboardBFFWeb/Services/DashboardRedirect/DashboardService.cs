using System.Net.Http.Headers;
using System.Text.Json;
using DashboardBff.Models.Dashboard;
using Microsoft.AspNetCore.Authentication;

namespace DashboardBff.Services.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<DashboardResponseDto> GetDashboardAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await httpContext.GetTokenAsync("access_token");
        Console.WriteLine("Access token exists: " + !string.IsNullOrWhiteSpace(accessToken));
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new UnauthorizedAccessException("Access token is missing.");
        }

        var inverterClient = _httpClientFactory.CreateClient("inverter-api");
        inverterClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var inverterResponse = await inverterClient.GetAsync("api/inverter/data", cancellationToken);

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