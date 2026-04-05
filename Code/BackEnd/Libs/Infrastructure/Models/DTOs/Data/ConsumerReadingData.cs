namespace Simulators.Models;

public record ConsumerReadingData(
    decimal Power,
    DateTime Timestamp, //Utc
    int AddressId
);