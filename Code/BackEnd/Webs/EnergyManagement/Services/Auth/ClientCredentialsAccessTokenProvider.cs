using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Polling.Services.Auth;

public sealed class ClientCredentialsAccessTokenProvider : IAccessTokenProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public ClientCredentialsAccessTokenProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTimeOffset.UtcNow < _expiresAt)
        {
            return _cachedToken;
        }

        var authority = _configuration["Authentication:Authority"];
        var clientId = _configuration["Authentication:ClientId"];
        var clientSecret = _configuration["Authentication:ClientSecret"];
        var scope = _configuration["Authentication:Scope"] ?? "inverter.read";

        if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return null;
        }

        var tokenEndpoint = $"{authority.TrimEnd('/')}/connect/token";
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["scope"] = scope
        };

        var client = _httpClientFactory.CreateClient();
        using var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form), cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to acquire service access token from {tokenEndpoint}. Status {(int)response.StatusCode}: {payload}");
        }

        using var doc = JsonDocument.Parse(payload);
        if (!doc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
        {
            throw new InvalidOperationException("Token endpoint response does not contain access_token.");
        }

        var token = accessTokenElement.GetString();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Token endpoint returned an empty access_token.");
        }

        var expiresIn = 300;
        if (doc.RootElement.TryGetProperty("expires_in", out var expiresInElement) && expiresInElement.TryGetInt32(out var parsedExpiresIn))
        {
            expiresIn = parsedExpiresIn;
        }

        _cachedToken = token;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, expiresIn - 30));

        return _cachedToken;
    }
}