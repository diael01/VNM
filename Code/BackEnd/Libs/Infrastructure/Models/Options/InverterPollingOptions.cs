public class InverterPollingOptions
{
    public int PollIntervalMinutes { get; set; } = 5; // default 5 minutes
    public string Source { get; set; } = "Simulator";

    // New fields for protocol selection
    public string Protocol { get; set; } = "Http";  // "Http" or "Tcp"
    public string HttpEndpoint { get; set; } = "http://localhost:5000/inverter/data";
    public string TcpHost { get; set; } = "127.0.0.1";
    public int TcpPort { get; set; } = 15000;
}
