namespace VNM.Infrastructure.Messages
{
    public record EnergyMessage
    {
        public string PanelId { get; init; } = default!;
        public double EnergyProduced { get; init; }
        public DateTime Timestamp { get; init; }
    }
}