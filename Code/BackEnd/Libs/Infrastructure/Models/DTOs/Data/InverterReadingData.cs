namespace Simulators.Models;

public record InverterData(
    decimal Power,
    decimal Voltage,
    decimal Current,
    DateTime Timestamp, //Utc
    int InverterInfoId,
    int AddressId
);

