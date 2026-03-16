using Models.Dashboard;
using Repositories.Models;

namespace Services.Redirect;

public interface IDashboardRedirectService
{
    Task<DashboardResponseDto> GetDashboardAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<List<InverterReading>> GetInverterReadingsAsync(string accessToken, CancellationToken cancellationToken = default);
}