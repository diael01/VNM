using MassTransit;
using EventBusCore.Events;

namespace EnergyManagement.Consumers;

public class MeterEventConsumer
    : IConsumer<MeterDataIngestedEvent>
{
    public Task Consume(ConsumeContext<MeterDataIngestedEvent> context)
    {
        var evt = context.Message;

        Console.WriteLine($"Meter {evt.MeterId} → {evt.Value}");

        return Task.CompletedTask;
    }
}

