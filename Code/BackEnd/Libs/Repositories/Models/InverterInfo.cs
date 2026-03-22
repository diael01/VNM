namespace Repositories.Models;

public class InverterInfo
{
    public int Id { get; set; }
    public string InverterType { get; set; }
    public string BatteryType { get; set; }
    public int NumberOfSolarPanels { get; set; }
    public string SolarPanelType { get; set; }
}
