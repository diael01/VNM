using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventBusClient
{
    public class EventBusClientService
    {
        private readonly HttpClient _http = new HttpClient();
        private readonly string _baseUrl;

        public EventBusClientService(string baseUrl = "https://localhost:7240/api/EventBus")
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public async Task PublishAsync(string topic, object message)
        {
            var json = JsonSerializer.Serialize(new { topic, message });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _http.PostAsync($"{_baseUrl}/publish", content);
        }
    }
}
