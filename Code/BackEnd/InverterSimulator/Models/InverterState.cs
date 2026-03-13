namespace InverterSimulator.Models;

public class InverterState
{
    public string SerialNumber { get; set; } = "SIM-INV-001";

    public decimal CurrentPowerWatts { get; set; }
    public decimal DailyEnergyKWh { get; set; }
    public decimal TotalEnergyKWh { get; set; }

    public decimal GridVoltage { get; set; }
    public decimal GridFrequencyHz { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public InverterStatus Status { get; set; }
}