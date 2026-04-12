using AutoMapper;
using Infrastructure.DTOs;
using Repositories.CRUD.Repositories;
using Repositories.Models;

namespace Services.Transfers;

public interface ITransferWorkflowCrudService
{
    Task<List<TransferWorkflowDto>> GetAllAsync();
    Task<TransferWorkflowDto?> GetByIdAsync(int id);
    Task<TransferWorkflowDto> CreateAsync(TransferWorkflowDto transferWorkflowDto);
    Task<TransferWorkflowDto> UpdateAsync(int id, TransferWorkflowDto transferWorkflowDto);
    Task<bool> DeleteAsync(int id);
}

public sealed class TransferWorkflowCrudService : ITransferWorkflowCrudService
{
    private readonly ITransferWorkflowRepository _transferWorkflowRepository;
    private readonly IMapper _mapper;

    public TransferWorkflowCrudService(ITransferWorkflowRepository transferWorkflowRepository, IMapper mapper)
    {
        _transferWorkflowRepository = transferWorkflowRepository;
        _mapper = mapper;
    }

    public async Task<List<TransferWorkflowDto>> GetAllAsync()
    {
        var workflows = await _transferWorkflowRepository.GetAllAsync();
        return _mapper.Map<List<TransferWorkflowDto>>(workflows);
    }

    public async Task<TransferWorkflowDto?> GetByIdAsync(int id)
    {
        var workflow = await _transferWorkflowRepository.GetByIdAsync(id);
        return workflow == null ? null : _mapper.Map<TransferWorkflowDto>(workflow);
    }

    public async Task<TransferWorkflowDto> CreateAsync(TransferWorkflowDto transferWorkflowDto)
    {
        var workflow = _mapper.Map<TransferWorkflow>(transferWorkflowDto);
        workflow.Id = 0;

        if (workflow.CreatedAtUtc == default)
        {
            workflow.CreatedAtUtc = DateTime.UtcNow;
        }

        if (workflow.EffectiveAtUtc == default)
        {
            workflow.EffectiveAtUtc = DateTime.UtcNow;
        }

        if (workflow.BalanceDayUtc == default)
        {
            workflow.BalanceDayUtc = DateTime.UtcNow.Date;
        }

        var created = await _transferWorkflowRepository.AddAsync(workflow);
        return _mapper.Map<TransferWorkflowDto>(created);
    }

    public async Task<TransferWorkflowDto> UpdateAsync(int id, TransferWorkflowDto transferWorkflowDto)
    {
        var workflow = _mapper.Map<TransferWorkflow>(transferWorkflowDto);
        workflow.Id = id;
        var updated = await _transferWorkflowRepository.UpdateAsync(workflow);
        return _mapper.Map<TransferWorkflowDto>(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _transferWorkflowRepository.DeleteAsync(id);
    }
}
