namespace InverterSimulator.Models;

public record InverterData(
    int PowerW,
    int VoltageV,
    int CurrentA,
    DateTime TimestampUtc
);
