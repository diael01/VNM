namespace Simulators.Configuration;

public class ConsumptionSimulatorOptions
{
    public int MinConsumption { get; set; } = 0;
    public int MaxConsumption { get; set; } = 10000;

    public int MinInverterId { get; set; } = 1;
    public int MaxInverterId  { get; set; } = 10;
}