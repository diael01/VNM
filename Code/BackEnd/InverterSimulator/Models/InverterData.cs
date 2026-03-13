namespace InverterSimulator.Models;

public record InverterData(
    int Power,
    int Voltage,
    int EnergyToday,
    DateTime Timestamp //Utc
);
