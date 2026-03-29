using Repositories.Models;
using Repositories.CRUD.Repositories;

namespace Services.Meter;

public interface IConsumptionReadingService
{
    Task<ConsumptionReading> CreateAsync(ConsumptionReading reading, CancellationToken cancellationToken = default);
    Task<ConsumptionReading?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConsumptionReading>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ConsumptionReading>> GetLatestAsync(int count, CancellationToken cancellationToken = default);
    Task<ConsumptionReading> UpdateAsync(ConsumptionReading reading, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class ConsumptionReadingService : IConsumptionReadingService
{
    private readonly IConsumptionReadingRepository _repository;

    public ConsumptionReadingService(IConsumptionReadingRepository repository)
    {
        _repository = repository;
    }

    public async Task<ConsumptionReading> CreateAsync(ConsumptionReading reading, CancellationToken cancellationToken = default)
    {
        return await _repository.AddAsync(reading, cancellationToken);
    }

    public async Task<ConsumptionReading?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<ConsumptionReading>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }

    public async Task<IEnumerable<ConsumptionReading>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _repository.GetLatestReadingsAsync(count, cancellationToken);
    }

    public async Task<ConsumptionReading> UpdateAsync(ConsumptionReading reading, CancellationToken cancellationToken = default)
    {
        return await _repository.UpdateAsync(reading, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }
}