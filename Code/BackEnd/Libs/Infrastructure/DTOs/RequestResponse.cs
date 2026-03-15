namespace Models.Dashboard;

public class GetDashboardStatsRequest
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class GetDashboardStatsResponse
{
    public int TotalMeters { get; set; }
    public int ActiveMeters { get; set; }
}
