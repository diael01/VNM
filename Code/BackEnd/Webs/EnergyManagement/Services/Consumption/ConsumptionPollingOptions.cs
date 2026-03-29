namespace ConsumptionPolling.Services
{
    public class ConsumptionPollingOptions
    {
        public string Protocol { get; set; } = "http";
        public string HttpEndpoint { get; set; } = string.Empty;
        public string Source { get; set; } = "Provider";
        public int PollIntervalMinutes { get; set; } = 5;
        // Add more options as needed for smart meter, etc.
    }
}
