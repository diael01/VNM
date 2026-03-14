using Repositories.Models;
using Repositories.CRUD.Repositories;

namespace Services.Inverter;

public interface IInverterReadingService
{
    Task<InverterReading> CreateAsync(InverterReading reading, CancellationToken cancellationToken = default);
    Task<InverterReading?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InverterReading>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<InverterReading>> GetLatestAsync(int count, CancellationToken cancellationToken = default);
    Task<InverterReading> UpdateAsync(InverterReading reading, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class InverterReadingService : IInverterReadingService
{
    private readonly IInverterReadingRepository _repository;

    public InverterReadingService(IInverterReadingRepository repository)
    {
        _repository = repository;
    }

    public async Task<InverterReading> CreateAsync(InverterReading reading, CancellationToken cancellationToken = default)
    {
        return await _repository.AddAsync(reading, cancellationToken);
    }

    public async Task<InverterReading?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<InverterReading>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }

    public async Task<IEnumerable<InverterReading>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _repository.GetLatestReadingsAsync(count, cancellationToken);
    }

    public async Task<InverterReading> UpdateAsync(InverterReading reading, CancellationToken cancellationToken = default)
    {
        return await _repository.UpdateAsync(reading, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }
}
