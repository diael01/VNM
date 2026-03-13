using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace InverterPolling.Services
{
    /// <summary>
    /// TCP-based inverter poller.
    /// Can be used with simulator or real inverter exposing TCP protocol.
    /// </summary>
    public class TcpInverterPoller : IInverterPoller
    {
        private readonly string _host;
        private readonly int _port;

        /// <summary>
        /// Creates a TCP poller
        /// </summary>
        /// <param name="host">IP or hostname of the inverter</param>
        /// <param name="port">TCP port</param>
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

                // Example: read 256 bytes (adjust to your protocol)
                var buffer = new byte[256];
                await ReadExactlyAsync(stream, buffer, ct);

                // TODO: parse your protocol. 
                // Here we assume the TCP simulator sends JSON for simplicity
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
                if (read == 0) break; // client closed connection
                totalRead += read;
            }
        }
    }
}
