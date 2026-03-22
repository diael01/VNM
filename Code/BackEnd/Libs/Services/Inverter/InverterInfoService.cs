using Repositories.Models;
using Repositories.CRUD.Repositories;

namespace Services.Inverter;

public interface IInverterInfoService
{
    Task<InverterInfo> CreateAsync(InverterInfo info, CancellationToken cancellationToken = default);
    Task<InverterInfo?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InverterInfo>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InverterInfo> UpdateAsync(InverterInfo info, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class InverterInfoService : IInverterInfoService
{
    private readonly IInverterInfoRepository _repository;

    public InverterInfoService(IInverterInfoRepository repository)
    {
        _repository = repository;
    }

    public async Task<InverterInfo> CreateAsync(InverterInfo info, CancellationToken cancellationToken = default)
    {
        return await _repository.AddAsync(info, cancellationToken);
    }

    public async Task<InverterInfo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<InverterInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }

    public async Task<InverterInfo> UpdateAsync(InverterInfo info, CancellationToken cancellationToken = default)
    {
        return await _repository.UpdateAsync(info, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }
}