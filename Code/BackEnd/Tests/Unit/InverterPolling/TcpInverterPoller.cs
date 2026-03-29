using Xunit;
using InverterPolling.Services;
using System.Net;
using System.Net.Sockets;
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
                var response = new byte[] { 0x01, 0x02, 0x03, 0x04 }; // fake data
                await stream.WriteAsync(response);
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
