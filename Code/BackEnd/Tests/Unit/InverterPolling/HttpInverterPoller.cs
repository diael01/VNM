using Xunit;
using Moq;
using InverterPolling.Services;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Json;
using Repositories.Models;

namespace InverterPolling.Tests.Polling
{
    public class HttpInverterPollerTests
    {
        [Fact]
        public async Task PollAsync_Should_ReturnReading()
        {
            var expected = new InverterReading
            {
                Power = 100,
                Voltage = 230,
                Current = 5,
                Timestamp = System.DateTime.UtcNow
            };

            var handler = new MockHttpMessageHandler(expected);
            var client = new HttpClient(handler);

            var poller = new HttpInverterPoller(client, "http://localhost/fake");

            var reading = await poller.PollAsync(CancellationToken.None);

            Assert.NotNull(reading);
            Assert.Equal(expected.Power, reading.Power);
            Assert.Equal(expected.Voltage, reading.Voltage);
        }
    }

    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly InverterReading _response;

        public MockHttpMessageHandler(InverterReading response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(_response)
            };
            return Task.FromResult(msg);
        }
    }
}
