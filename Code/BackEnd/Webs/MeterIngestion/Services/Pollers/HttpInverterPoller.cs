using System.Net.Http;
using System.Text.Json;

namespace InverterPolling.Services
{
    public class HttpInverterPoller : IInverterPoller
    {
        private readonly HttpClient _client;
        private readonly string _endpoint;
        private readonly string _source;

        public HttpInverterPoller(HttpClient client, string endpoint, string source = "HTTP")
        {
            _client = client;
            _endpoint = endpoint;
            _source = source;
        }

        public async Task<InverterReading?> PollAsync(CancellationToken ct = default)
        {
            var response = await _client.GetAsync(_endpoint, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var data = JsonSerializer.Deserialize<InverterReading>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data is null) return null;

            return new InverterReading
            {
                TimestampUtc = data.TimestampUtc,
                PowerW = data.PowerW,
                VoltageV = data.VoltageV,
                CurrentA = data.CurrentA,
                Source = _source
            };
        }

    }
}
