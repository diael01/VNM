namespace Simulators.Configuration;

public class ConsumptionSimulatorOptions
{
    public decimal MinConsumption { get; set; } = 0m;
    public decimal MaxConsumption { get; set; } = 10000.1111m;

    public int MinInverterId { get; set; } = 1;
    public int MaxInverterId  { get; set; } = 10;
}