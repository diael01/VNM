namespace Simulators.Models;

public record InverterData(
    int Power,
    int Voltage,
    int Current,
    DateTime Timestamp, //Utc
    int InverterInfoId
);

