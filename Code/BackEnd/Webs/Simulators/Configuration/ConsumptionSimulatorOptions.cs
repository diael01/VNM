namespace Simulators.Configuration;

public class ConsumptionSimulatorOptions
{
    public int MinConsumption { get; set; } = 0;
    public int MaxConsumption { get; set; } = 10000;

    public int LocationId { get; set; } = 1;
}