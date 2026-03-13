using DashboardBff.Models.Dashboard;

namespace DashboardBff.Services.Dashboard;

public interface IDashboardService
{
    Task<DashboardResponseDto> GetDashboardAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}