using Repositories.Models;
using Repositories.CRUD.Repositories;

namespace Services.Inverter;

public interface IAddressService
{
    Task<Address> CreateAsync(Address address, CancellationToken cancellationToken = default);
    Task<Address?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Address>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Address> UpdateAsync(Address address, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class AddressService : IAddressService
{
    private readonly IAddressRepository _repository;

    public AddressService(IAddressRepository repository)
    {
        _repository = repository;
    }

    public async Task<Address> CreateAsync(Address address, CancellationToken cancellationToken = default)
    {
        return await _repository.AddAsync(address, cancellationToken);
    }

    public async Task<Address?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<Address>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }

    public async Task<Address> UpdateAsync(Address address, CancellationToken cancellationToken = default)
    {
        return await _repository.UpdateAsync(address, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }
}