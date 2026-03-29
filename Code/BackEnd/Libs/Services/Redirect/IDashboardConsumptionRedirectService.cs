using Repositories.Models;

namespace Services.Redirect;

public interface IDashboardConsumptionRedirectService
{
    Task<List<ConsumptionReading>> GetAllConsumptionReadingsAsync(string accessToken, CancellationToken cancellationToken = default);
     Task<ConsumptionReading?> GetConsumptionReadingByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default);
    Task<ConsumptionReading> CreateConsumptionReadingAsync(string accessToken, ConsumptionReading reading, CancellationToken cancellationToken = default);
    Task<ConsumptionReading> UpdateConsumptionReadingAsync(string accessToken, int id, ConsumptionReading reading, CancellationToken cancellationToken = default);
    Task<bool> DeleteConsumptionReadingAsync(string accessToken, int id, CancellationToken cancellationToken = default);
 }
