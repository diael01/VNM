using MassTransit;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace EventBusTestHarness.Consumers;

public class MeterIngestionConsumer : IConsumer<EnergyMessage>
{
    private static readonly ActivitySource Source = new("MeterIngestionConsumer");

    public async Task Consume(ConsumeContext<EnergyMessage> context)
    {
        using var activity = Source.StartActivity("ConsumeEnergyMessage");
        activity?.SetTag("energy.amount", context.Message.Amount);

        Console.WriteLine($"[MeterIngestionConsumer] Received energy: {context.Message.Amount} at {context.Message.Timestamp}");
        await Task.CompletedTask;
    }
}
