public record MeterIngestRequest
{
    public string MeterId { get; init; } = default!;
    public decimal Value { get; init; }
}
