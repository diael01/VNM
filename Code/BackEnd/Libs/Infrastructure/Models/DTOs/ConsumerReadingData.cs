namespace Simulators.Models;

public record ConsumerReadingData(
    int Power,
    DateTime Timestamp, //Utc
    int AddressId
);