using Models.Dashboard;

namespace Services.Redirect;

public interface IDashboardRedirectService
{
    Task<DashboardResponseDto> GetDashboardAsync(string accessToken, CancellationToken cancellationToken = default);
}