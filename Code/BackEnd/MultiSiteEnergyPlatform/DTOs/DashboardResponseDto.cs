namespace DashboardBff.Models.Dashboard;

public sealed class DashboardResponseDto
{
    public InverterDataDto? Inverter { get; set; }
}

public sealed class InverterDataDto
{
    public int Power { get; set; }
    public int Voltage { get; set; }
    public int Current { get; set; }
    public DateTime Timestamp { get; set; }
}