using MassTransit;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace EventBusTestHarness.Consumers;

public class DashboardConsumer : IConsumer<EnergyMessage>
{
    private static readonly ActivitySource Source = new("DashboardConsumer");

    public async Task Consume(ConsumeContext<EnergyMessage> context)
    {
        using var activity = Source.StartActivity("ConsumeDashboardMessage");
        activity?.SetTag("dashboard.processed", true);

        Console.WriteLine($"[DashboardConsumer] Processed energy: {context.Message.Amount} at {context.Message.Timestamp}");
        await Task.CompletedTask;
    }
}
