namespace Simulators.Configuration;

public class InverterSimulatorOptions
{
    public decimal MinPower { get; set; } = 0;
    public decimal MaxPower { get; set; } = 5000.55m;

    public decimal MinVoltage { get; set; } = 200.22m;
    public decimal MaxVoltage { get; set; } = 250.55m;

    public decimal MinCurrent { get; set; } = 0m;
    public decimal MaxCurrent { get; set; } = 20.22m;

     public int MinInverterId { get; set; } = 1;
    public int MaxInverterId { get; set; } = 10;
}

