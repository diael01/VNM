using Microsoft.Extensions.DependencyInjection;
using MassTransit.Testing;
using Xunit;
using System.Threading.Tasks;
using MassTransit.Testing;
using Microsoft.Extensions.Hosting;
using EventBusTestHarness;

public class MeterIngestionTests
{
    [Fact]
    public async Task Should_Send_EnergyMessage()
    {
        var host = Host.CreateDefaultBuilder()
            .AddTestEventBus()
            .Build();

        await host.StartAsync();

      /*   var provider = host.Services.BuildServiceProvider();
        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start(); */
        var harness = host.Services.GetRequiredService<ITestHarness>();

        var bus = host.Services.GetRequiredService<IBus>();
        // Send a test message
        //var bus = provider.GetRequiredService<IBus>();
        //await bus.Publish(new EnergyMessage(123));

await bus.Publish(new EnergyMessage { Amount = 123.4, Timestamp = DateTime.UtcNow });

// Wait for consumers to consume the message
//var consumed = await harness.Consumed.Any<EnergyMessage>();
//Console.WriteLine($"Message consumed: {consumed}");


        // Assert it was published
        Assert.True(await harness.Published.Any<EnergyMessage>());

        // Optionally assert it was consumed
        Assert.True(await harness.Consumed.Any<EnergyMessage>());

        //await harness.Stop();

        await host.StopAsync();
    }
}
