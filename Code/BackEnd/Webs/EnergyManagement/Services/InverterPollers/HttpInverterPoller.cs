using System.Net.Http.Headers;
using System.Text.Json;
using Polling.Services.Auth;
using Repositories.Models;

namespace InverterPolling.Services
{
    public class HttpInverterPoller : IInverterPoller
    {
        private readonly HttpClient _client;
        private readonly string _endpoint;
        private readonly string _source;
        private readonly IAccessTokenProvider _accessTokenProvider;

        public HttpInverterPoller(
            HttpClient client,
            string endpoint,
            IAccessTokenProvider accessTokenProvider,
            string source = "HTTP")
        {
            _client = client;
            _endpoint = endpoint;
            _accessTokenProvider = accessTokenProvider;
            _source = source;
        }

        public async Task<InverterReading?> PollAsync(CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _endpoint);
            var accessToken = await _accessTokenProvider.GetAccessTokenAsync(ct);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            using var response = await _client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var data = JsonSerializer.Deserialize<InverterReading>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data is null) return null;

            return new InverterReading
            {
                Timestamp = data.Timestamp,
                Power = data.Power,
                Voltage = data.Voltage,
                Current = data.Current,
                Source = _source
            };
        }
    }
}
