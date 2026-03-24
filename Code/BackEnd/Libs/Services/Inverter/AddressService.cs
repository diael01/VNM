using Repositories.Models;
using Repositories.CRUD.Repositories;
using Infrastructure.DTOs;
using AutoMapper;

namespace Services.Inverter;

public interface IAddressService
{
    Task<Address> CreateAsync(AddressDto addressDto, CancellationToken cancellationToken = default);
    Task<Address?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Address>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Address> UpdateAsync(int id, AddressDto addressDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class AddressService : IAddressService
{
    private readonly IAddressRepository _repository;
    private readonly IMapper _mapper;

    public AddressService(IAddressRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Address> CreateAsync(AddressDto addressDto, CancellationToken cancellationToken = default)
    {
        var address = _mapper.Map<Address>(addressDto);
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

    public async Task<Address> UpdateAsync(int id, AddressDto addressDto, CancellationToken cancellationToken = default)
    {
        var address = _mapper.Map<Address>(addressDto);
        address.Id = id;
        return await _repository.UpdateAsync(address, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }
}