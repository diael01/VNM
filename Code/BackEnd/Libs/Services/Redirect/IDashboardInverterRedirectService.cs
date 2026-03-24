using Models.Dashboard;
using Repositories.Models;

namespace Services.Redirect;

using Repositories.Models;

public interface IDashboardInverterRedirectService
{
    Task<List<InverterReading>> GetInverterReadingsAsync(string accessToken, CancellationToken cancellationToken = default);

    // InverterInfo CRUD
    Task<List<InverterInfo>> GetAllInverterInfoAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<InverterInfo?> GetInverterInfoByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default);
    Task<InverterInfo> CreateInverterInfoAsync(string accessToken, InverterInfo info, CancellationToken cancellationToken = default);
    Task<InverterInfo> UpdateInverterInfoAsync(string accessToken, int id, InverterInfo info, CancellationToken cancellationToken = default);
    Task<bool> DeleteInverterInfoAsync(string accessToken, int id, CancellationToken cancellationToken = default);
}
