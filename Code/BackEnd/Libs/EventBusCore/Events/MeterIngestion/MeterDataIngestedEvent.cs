namespace EventBusCore.Events;

public record MeterDataIngestedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string MeterId { get; init; } = default!;
    public decimal Value { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
