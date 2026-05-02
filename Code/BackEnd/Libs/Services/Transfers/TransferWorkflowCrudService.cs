using AutoMapper;
using Infrastructure.DTOs;
using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Repositories;
using Repositories.Models;

namespace Services.Transfers;

public interface ITransferWorkflowCrudService
{
    Task<List<TransferWorkflowDto>> GetAllAsync();
    Task<List<TransferWorkflowStatusHistoryDto>> GetAllHistoryAsync();
    Task<TransferWorkflowDto?> GetByIdAsync(int id);
    Task<List<TransferWorkflowStatusHistoryDto>> GetHistoryAsync(int id);
    Task<TransferWorkflowDto> CreateAsync(TransferWorkflowDto transferWorkflowDto);
    Task<TransferWorkflowDto> UpdateAsync(int id, TransferWorkflowDto transferWorkflowDto);
    Task<TransferWorkflowDto> ApproveAsync(int id, string? note = null);
    Task<TransferWorkflowDto> RejectAsync(int id, string? note = null);
    Task<TransferWorkflowDto> ExecuteAsync(int id, string? note = null);
    Task<TransferWorkflowDto> SettleAsync(int id, string? note = null);
    Task<bool> DeleteAsync(int id);
}

public sealed class TransferWorkflowCrudService : ITransferWorkflowCrudService
{
    private const int StatusPlanned = 0;
    private const int StatusApproved = 1;
    private const int StatusExecuted = 2;
    private const int StatusSettled = 3;
    private const int StatusRejected = 4;
    private const int StatusCancelled = 5;
    private const int StatusFailed = 6;

    private readonly ITransferWorkflowRepository _transferWorkflowRepository;
    private readonly IMapper _mapper;
    private readonly VnmDbContext _dbContext;

    public TransferWorkflowCrudService(
        ITransferWorkflowRepository transferWorkflowRepository,
        IMapper mapper,
        VnmDbContext dbContext)
    {
        _transferWorkflowRepository = transferWorkflowRepository;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<List<TransferWorkflowDto>> GetAllAsync()
    {
        var workflows = await _transferWorkflowRepository.GetAllAsync();
        return _mapper.Map<List<TransferWorkflowDto>>(workflows);
    }

    public async Task<List<TransferWorkflowStatusHistoryDto>> GetAllHistoryAsync()
    {
        var history = await _dbContext.TransferWorkflowStatusHistory
            .AsNoTracking()
            .OrderByDescending(h => h.CreatedAtUtc)
            .ToListAsync();

        return _mapper.Map<List<TransferWorkflowStatusHistoryDto>>(history);
    }

    public async Task<TransferWorkflowDto?> GetByIdAsync(int id)
    {
        var workflow = await _transferWorkflowRepository.GetByIdAsync(id);
        return workflow == null ? null : _mapper.Map<TransferWorkflowDto>(workflow);
    }

    public async Task<List<TransferWorkflowStatusHistoryDto>> GetHistoryAsync(int id)
    {
        var history = await _dbContext.TransferWorkflowStatusHistory
            .AsNoTracking()
            .Where(h => h.TransferWorkflowId == id)
            .OrderBy(h => h.CreatedAtUtc)
            .ToListAsync();

        return _mapper.Map<List<TransferWorkflowStatusHistoryDto>>(history);
    }

    public async Task<TransferWorkflowDto> CreateAsync(TransferWorkflowDto transferWorkflowDto)
    {
        var workflow = _mapper.Map<TransferWorkflow>(transferWorkflowDto);
        workflow.Id = 0;
        workflow.Status = StatusPlanned;

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

        _dbContext.TransferWorkflowStatusHistory.Add(new TransferWorkflowStatusHistory
        {
            TransferWorkflow = workflow,
            FromStatus = null,
            ToStatus = workflow.Status,
            Note = "Created",
        });

        var created = await _transferWorkflowRepository.AddAsync(workflow);
        return _mapper.Map<TransferWorkflowDto>(created);
    }

    public async Task<TransferWorkflowDto> UpdateAsync(int id, TransferWorkflowDto transferWorkflowDto)
    {
        var existing = await _transferWorkflowRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Transfer workflow not found.");

        var candidate = _mapper.Map<TransferWorkflow>(transferWorkflowDto);
        candidate.Id = id;

        if (existing.Status != candidate.Status)
        {
            throw new InvalidOperationException("Status updates are command-driven. Use approve/reject/execute/settle endpoints.");
        }

        if (existing.Status != StatusPlanned && HasLockedPlanningFieldsChanged(existing, candidate))
        {
            throw new InvalidOperationException("Approved/executed workflows are frozen. Core transfer values cannot be edited.");
        }

        // Preserve immutable creation audit fields from the existing entity.
        candidate.CreatedAtUtc = existing.CreatedAtUtc;
        candidate.CreatedBy = existing.CreatedBy;

        var updated = await _transferWorkflowRepository.UpdateAsync(candidate);
        return _mapper.Map<TransferWorkflowDto>(updated);
    }

    public Task<TransferWorkflowDto> ApproveAsync(int id, string? note = null)
        => TransitionStatusAsync(id, StatusApproved, string.IsNullOrWhiteSpace(note) ? "Workflow has been approved" : note);

    public Task<TransferWorkflowDto> RejectAsync(int id, string? note = null)
        => TransitionStatusAsync(id, StatusRejected, string.IsNullOrWhiteSpace(note) ? "Workflow has been rejected" : note);

    public Task<TransferWorkflowDto> ExecuteAsync(int id, string? note = null)
        => TransitionStatusAsync(id, StatusExecuted, note);

    public Task<TransferWorkflowDto> SettleAsync(int id, string? note = null)
        => TransitionStatusAsync(id, StatusSettled, note);

    public async Task<bool> DeleteAsync(int id)
    {
        return await _transferWorkflowRepository.DeleteAsync(id);
    }

    private static bool HasLockedPlanningFieldsChanged(TransferWorkflow existing, TransferWorkflow candidate)
    {
        return existing.SourceAddressId != candidate.SourceAddressId
            || existing.DestinationAddressId != candidate.DestinationAddressId
            || existing.AmountKwh != candidate.AmountKwh
            || existing.SourceSurplusKwhAtWorkflow != candidate.SourceSurplusKwhAtWorkflow
            || existing.DestinationDeficitKwhAtWorkflow != candidate.DestinationDeficitKwhAtWorkflow
            || existing.RemainingSourceSurplusKwhAfterWorkflow != candidate.RemainingSourceSurplusKwhAfterWorkflow;
    }

    private async Task<TransferWorkflowDto> TransitionStatusAsync(int id, int toStatus, string? note)
    {
        var workflow = await _transferWorkflowRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Transfer workflow not found.");

        if (!IsValidStatusTransition(workflow.Status, toStatus))
        {
            throw new InvalidOperationException($"Invalid status transition: {workflow.Status} -> {toStatus}.");
        }

        if (workflow.Status == toStatus)
        {
            return _mapper.Map<TransferWorkflowDto>(workflow);
        }

        var fromStatus = workflow.Status;
        workflow.Status = toStatus;

        _dbContext.TransferWorkflowStatusHistory.Add(new TransferWorkflowStatusHistory
        {
            TransferWorkflowId = workflow.Id,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Note = note,
        });

        var updated = await _transferWorkflowRepository.UpdateAsync(workflow);
        return _mapper.Map<TransferWorkflowDto>(updated);
    }

    private static bool IsValidStatusTransition(int from, int to)
    {
        if (from == to) return true;

        return from switch
        {
            StatusPlanned => to is StatusApproved or StatusRejected,
            StatusApproved => to is StatusExecuted or StatusCancelled,
            StatusExecuted => to is StatusSettled or StatusFailed,
            StatusFailed => to is StatusExecuted or StatusCancelled,
            StatusRejected => false,
            StatusCancelled => to is StatusPlanned,
            StatusSettled => false,
            _ => false,
        };
    }
}
