namespace EventBusTestHarness;

public record EnergyMessage
{
    public double Amount { get; init; }
    public DateTime Timestamp { get; init; }
}
