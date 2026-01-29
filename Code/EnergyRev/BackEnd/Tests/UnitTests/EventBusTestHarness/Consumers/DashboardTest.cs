using Microsoft.Extensions.DependencyInjection;
using MassTransit.Testing;
using Xunit;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using EventBusTestHarness;
using EventBusCore.Events; // <-- Make sure this contains DashboardStatusEvent
using MassTransit;

namespace EventBusTestHarness.Tests
{
    public class DashboardTests
    {
        [Fact]
        public async Task Should_Send_DashboardStatusEvent()
        {
            // Arrange
            var host = Host.CreateDefaultBuilder()
                .AddTestEventBus() // sets up the in-memory MassTransit harness
                .Build();

            await host.StartAsync();

            var harness = host.Services.GetRequiredService<ITestHarness>();
            var bus = host.Services.GetRequiredService<IBus>();

            // Act: publish the strongly-typed event
            var dashboardEvent = new DashboardStatusEvent
            {
                Message = "Running",
            };

            await bus.Publish(dashboardEvent);

            // Assert: verify it was published and consumed
            Assert.True(await harness.Published.Any<DashboardStatusEvent>());
            Assert.True(await harness.Consumed.Any<DashboardStatusEvent>());

            await host.StopAsync();
        }
    }
}
