using AutoMapper;
using Infrastructure.DTOs;
using Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Repositories;
using Repositories.Models;
using EnergyManagement.Services.Providers;

namespace Services.Transfers;

public interface ITransitionWorkflowService
{
    Task<List<TransferWorkflowDto>> GetAllAsync(CancellationToken ct = default);
    Task<List<TransferWorkflowStatusHistoryDto>> GetAllHistoryAsync(CancellationToken ct = default);
    Task<TransferWorkflowDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<TransferWorkflowStatusHistoryDto>> GetHistoryAsync(int id, CancellationToken ct = default);
    Task<TransferWorkflowDto> ApproveAsync(int id, string? note = null, CancellationToken ct = default);
    Task<TransferWorkflowDto> RejectAsync(int id, string? note = null, CancellationToken ct = default);
    Task<TransferWorkflowDto> ExecuteAsync(int id, string? note = null, CancellationToken ct = default);
    Task<TransferWorkflowDto> SettleAsync(int id, string? note = null, CancellationToken ct = default);
}

public sealed class TransitionWorkflowService : ITransitionWorkflowService
{
    private readonly ITransferWorkflowRepository _transferWorkflowRepository;
    private readonly IMapper _mapper;
    private readonly VnmDbContext _dbContext;
    private readonly IProviderSettlementService _providerSettlementService;

    public TransitionWorkflowService(
        ITransferWorkflowRepository transferWorkflowRepository,
        IMapper mapper,
        VnmDbContext dbContext,
        IProviderSettlementService providerSettlementService)
    {
        _transferWorkflowRepository = transferWorkflowRepository;
        _mapper = mapper;
        _dbContext = dbContext;
        _providerSettlementService = providerSettlementService;
    }

    public async Task<List<TransferWorkflowDto>> GetAllAsync(CancellationToken ct = default)
    {
        var workflows = await _transferWorkflowRepository.GetAllAsync();
        return _mapper.Map<List<TransferWorkflowDto>>(workflows);
    }

    public async Task<List<TransferWorkflowStatusHistoryDto>> GetAllHistoryAsync(CancellationToken ct = default)
    {
        var history = await _dbContext.TransferWorkflowStatusHistory
            .AsNoTracking()
            .OrderByDescending(h => h.CreatedAtUtc)
            .Select(h => new TransferWorkflowStatusHistoryDto
            {
                Id = h.Id,
                TransferWorkflowId = h.TransferWorkflowId,
                SourceAddressId = h.TransferWorkflow.SourceAddressId,
                DestinationAddressId = h.TransferWorkflow.DestinationAddressId,
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                Note = h.Note,
                CreatedAtUtc = h.CreatedAtUtc,
                CreatedBy = h.CreatedBy
            })
            .ToListAsync(ct);

        return history;
    }

    public async Task<TransferWorkflowDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var workflow = await _transferWorkflowRepository.GetByIdAsync(id);
        return workflow == null ? null : _mapper.Map<TransferWorkflowDto>(workflow);
    }

    public async Task<List<TransferWorkflowStatusHistoryDto>> GetHistoryAsync(int id, CancellationToken ct = default)
    {
        var history = await _dbContext.TransferWorkflowStatusHistory
            .AsNoTracking()
            .Where(h => h.TransferWorkflowId == id)
            .OrderBy(h => h.CreatedAtUtc)
            .Select(h => new TransferWorkflowStatusHistoryDto
            {
                Id = h.Id,
                TransferWorkflowId = h.TransferWorkflowId,
                SourceAddressId = h.TransferWorkflow.SourceAddressId,
                DestinationAddressId = h.TransferWorkflow.DestinationAddressId,
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                Note = h.Note,
                CreatedAtUtc = h.CreatedAtUtc,
                CreatedBy = h.CreatedBy
            })
            .ToListAsync(ct);

        return history;
    }

    public Task<TransferWorkflowDto> ApproveAsync(int id, string? note = null, CancellationToken ct = default)
        => TransitionStatusAsync(id, TransferStatus.Approved, string.IsNullOrWhiteSpace(note) ? "Workflow has been approved" : note, ct);

    public Task<TransferWorkflowDto> RejectAsync(int id, string? note = null, CancellationToken ct = default)
        => TransitionStatusAsync(id, TransferStatus.Rejected, string.IsNullOrWhiteSpace(note) ? "Workflow has been rejected" : note, ct);

    public Task<TransferWorkflowDto> ExecuteAsync(int id, string? note = null, CancellationToken ct = default)
        => TransitionStatusAsync(id, TransferStatus.Executed, string.IsNullOrWhiteSpace(note) ? "Workflow has been executed" : note, ct);

    public async Task<TransferWorkflowDto> SettleAsync(int id, string? note = null, CancellationToken ct = default)
    {
        var workflow = await _transferWorkflowRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Transfer workflow {id} was not found.");

        if (workflow.Status == (int)TransferStatus.Settled)
            return _mapper.Map<TransferWorkflowDto>(workflow);

        var fromStatus = (TransferStatus)workflow.Status;
        if (!IsValidStatusTransition(fromStatus, TransferStatus.Settled))
            throw new InvalidOperationException($"Invalid status transition: {(int)fromStatus} -> {(int)TransferStatus.Settled}.");

        var effectiveNote = string.IsNullOrWhiteSpace(note) ? "Workflow has been settled" : note;
        await _providerSettlementService.SettleWorkflowAsync(id, effectiveNote, ct);

        var updatedWorkflow = await _transferWorkflowRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Transfer workflow {id} was not found.");

        return _mapper.Map<TransferWorkflowDto>(updatedWorkflow);
    }



    private async Task<TransferWorkflowDto> TransitionStatusAsync(int id, TransferStatus toStatus, string? note, CancellationToken ct = default)
    {
        var workflow = await _transferWorkflowRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Transfer workflow {id} was not found.");

        var fromStatus = (TransferStatus)workflow.Status;
        if (!IsValidStatusTransition(fromStatus, toStatus))
            throw new InvalidOperationException($"Invalid status transition: {(int)fromStatus} -> {(int)toStatus}.");
        
        if (workflow.Status == (int)toStatus)
            return _mapper.Map<TransferWorkflowDto>(workflow);
        
        var nowUtc = DateTime.UtcNow;
        workflow.Status = (int)toStatus;
        workflow.EffectiveAtUtc = nowUtc;
        workflow.UpdatedAtUtc = nowUtc;
        workflow.UpdatedBy = "system"; //todo: get the user from context

        if (toStatus == TransferStatus.Executed)
        {
                workflow.SourceSurplusKwhAtExecution = decimal.Round(
                Math.Max(0m, workflow.SourceSurplusKwhAtWorkflow - workflow.AmountKwh),
                4);
                workflow.DestinationDeficitKwhAtExecution = decimal.Round(
                Math.Max(0m, workflow.DestinationDeficitKwhAtWorkflow - workflow.AmountKwh),
                4);
                workflow.AmountAtExecutionKwh = workflow.AmountKwh;
        }

        _dbContext.TransferWorkflowStatusHistory.Add(new TransferWorkflowStatusHistory
        {
            TransferWorkflowId = workflow.Id,
            FromStatus = (int)fromStatus,
            ToStatus = (int)toStatus,
            Note = note,
            UpdatedAtUtc = nowUtc,
            UpdatedBy = "system"  //todo: get the user from context          
        });

        var updated = await _transferWorkflowRepository.UpdateAsync(workflow, ct);
        return _mapper.Map<TransferWorkflowDto>(updated);
    }

    private static bool IsValidStatusTransition(TransferStatus from, TransferStatus to)
    {
        if (from == to) return true;

        return from switch
        {
            TransferStatus.Planned => to is TransferStatus.Approved or TransferStatus.Rejected,
            TransferStatus.Approved => to is TransferStatus.Executed or TransferStatus.Rejected,
            TransferStatus.Executed => to is TransferStatus.Settled or TransferStatus.Failed,
            TransferStatus.Failed => to is TransferStatus.Executed or TransferStatus.Rejected,
            TransferStatus.Rejected => false,
            TransferStatus.Settled => false,
            _ => false,
        };
    }
}
