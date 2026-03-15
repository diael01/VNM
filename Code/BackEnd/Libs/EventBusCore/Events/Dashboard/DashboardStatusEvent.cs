namespace EventBusCore.Events
{
    public class DashboardStatusEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
