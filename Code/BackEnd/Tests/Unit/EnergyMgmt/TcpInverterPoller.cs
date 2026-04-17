using Xunit;
using InverterPolling.Services;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InverterPolling.Services.InverterPoller.Services;

namespace InverterPolling.Tests.Polling
{
    public class TcpInverterPollerTests
    {
        [Fact]
        public async Task PollAsync_Should_ReadFromTcpSimulators()
        {
            // Arrange: start a TCP Simulators
            var listener = new TcpListener(IPAddress.Loopback, 6000);
            listener.Start();

            _ = Task.Run(async () =>
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                var json = "{\"id\":1,\"inverterInfoId\":1,\"power\":123.4,\"timestamp\":\"2026-01-01T00:00:00Z\"}";
                var payload = Encoding.UTF8.GetBytes(json);
                await stream.WriteAsync(payload, 0, payload.Length);
            });

            var poller = new TcpInverterPoller("127.0.0.1", 6000);

            // Act
            var reading = await poller.PollAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(reading);

            listener.Stop();
        }
    }
}
