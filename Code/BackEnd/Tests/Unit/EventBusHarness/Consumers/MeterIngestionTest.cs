using Microsoft.Extensions.DependencyInjection;
using MassTransit.Testing;
using Xunit;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using EventBusTestHarness;
using EventBusCore.Events; // <-- Use your new event DTOs
using MassTransit;

namespace EventBusTestHarness.Tests;

public class EnergyManagementTests
{
    [Fact]
    public async Task Should_Send_MeterDataIngestedEvent()
    {
        // Arrange
        var host = Host.CreateDefaultBuilder()
            .AddTestEventBus() // sets up the in-memory test harness
            .Build();

        await host.StartAsync();

        var harness = host.Services.GetRequiredService<ITestHarness>();
        var bus = host.Services.GetRequiredService<IBus>();

        // Act: publish the new strongly-typed event
        var testEvent = new MeterDataIngestedEvent
        {
            MeterId = "MTR-001",
            Value = 123.4M,
        };

        await bus.Publish(testEvent);

        // Assert: verify the event was published and consumed
        Assert.True(await harness.Published.Any<MeterDataIngestedEvent>());
        Assert.True(await harness.Consumed.Any<MeterDataIngestedEvent>());

        await host.StopAsync();
    }
}
