using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Repositories.Models;

namespace InverterPolling.Services.InverterPoller.Services
{
    public class TcpInverterPoller : IInverterPoller
    {
        private readonly string _host;
        private readonly int _port;

        public TcpInverterPoller(string host = "localhost", int port = 15000)
        {
            _host = host;
            _port = port;
        }

        public async Task<InverterReading?> PollAsync(CancellationToken ct = default)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_host, _port, ct);
                using var stream = client.GetStream();

                var buffer = new byte[256];
                await ReadExactlyAsync(stream, buffer, ct);

                var jsonLength = Array.IndexOf(buffer, (byte)0);
                var jsonBytes = jsonLength > 0 ? buffer[..jsonLength] : buffer;
                var json = System.Text.Encoding.UTF8.GetString(jsonBytes);

                var reading = JsonSerializer.Deserialize<InverterReading>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (reading != null)
                    reading.Source = "TCP";

                return reading;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TcpInverterPoller] Error: {ex.Message}");
                return null;
            }
        }

        private static async Task ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(totalRead), ct);
                if (read == 0) break;
                totalRead += read;
            }
        }
    }
}
