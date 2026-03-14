namespace InverterSimulatorWeb.Configuration;

public class InverterSimulatorWebOptions
{
    public int MinPower { get; set; } = 0;
    public int MaxPower { get; set; } = 5000;

    public int MinVoltage { get; set; } = 200;
    public int MaxVoltage { get; set; } = 250;

    public int MinCurrent { get; set; } = 0;
    public int MaxCurrent { get; set; } = 20;
}

