namespace VNM.Infrastructure.Configuration;

public sealed class EndpointsOptions
{
    public string InverterApi { get; set; } = string.Empty;
    public string MeterApi { get; set; } = string.Empty;
}