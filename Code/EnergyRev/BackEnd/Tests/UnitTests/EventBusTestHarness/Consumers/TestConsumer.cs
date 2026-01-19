using MassTransit;
using System.Threading.Tasks;

public class TestConsumer : IConsumer<EnergyMessage>
{
    public Task Consume(ConsumeContext<EnergyMessage> context)
    {
        // just log or count messages for test
        Console.WriteLine($"Received energy value: {context.Message.Value}");
        return Task.CompletedTask;
    }
}

public record EnergyMessage(int Value);
