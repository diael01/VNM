using Repositories.Models;
using Repositories.CRUD.Repositories;
using Infrastructure.DTOs;
using AutoMapper;

namespace Services.Inverter;

public interface IInverterInfoService
{
    Task<InverterInfoDto> CreateAsync(InverterInfoDto dto, CancellationToken cancellationToken = default);
    Task<InverterInfoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InverterInfoDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InverterInfoDto> UpdateAsync(int id, InverterInfoDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class InverterInfoService : IInverterInfoService
{
    private readonly IInverterInfoRepository _repository;
    private readonly IMapper _mapper;

    public InverterInfoService(IInverterInfoRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<InverterInfoDto> CreateAsync(InverterInfoDto dto, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<InverterInfo>(dto);
        var created = await _repository.AddAsync(entity, cancellationToken);
        return _mapper.Map<InverterInfoDto>(created);
    }

    public async Task<InverterInfoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity == null ? null : _mapper.Map<InverterInfoDto>(entity);
    }

    public async Task<IEnumerable<InverterInfoDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return entities.Select(_mapper.Map<InverterInfoDto>);
    }

    public async Task<InverterInfoDto> UpdateAsync(int id, InverterInfoDto dto, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<InverterInfo>(dto);
        entity.Id = id;
        var updated = await _repository.UpdateAsync(entity, cancellationToken);
        return _mapper.Map<InverterInfoDto>(updated);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }
}