using AutoMapper;
using Infrastructure.DTOs;
using Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Repositories;
using Repositories.Models;

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

    public TransitionWorkflowService(
        ITransferWorkflowRepository transferWorkflowRepository,
        IMapper mapper,
        VnmDbContext dbContext)
    {
        _transferWorkflowRepository = transferWorkflowRepository;
        _mapper = mapper;
        _dbContext = dbContext;
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
            .ToListAsync(ct);

        return _mapper.Map<List<TransferWorkflowStatusHistoryDto>>(history);
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
            .ToListAsync(ct);

        return _mapper.Map<List<TransferWorkflowStatusHistoryDto>>(history);
    }

    public Task<TransferWorkflowDto> ApproveAsync(int id, string? note = null, CancellationToken ct = default)
        => TransitionStatusAsync(id, StatusApproved, string.IsNullOrWhiteSpace(note) ? "Workflow has been approved" : note, ct);

    public Task<TransferWorkflowDto> RejectAsync(int id, string? note = null, CancellationToken ct = default)
        => TransitionStatusAsync(id, StatusRejected, string.IsNullOrWhiteSpace(note) ? "Workflow has been rejected" : note, ct);

    public Task<TransferWorkflowDto> ExecuteAsync(int id, string? note = null, CancellationToken ct = default)
        => TransitionStatusAsync(id, StatusExecuted, string.IsNullOrWhiteSpace(note) ? "Workflow has been executed" : note, ct);

    public Task<TransferWorkflowDto> SettleAsync(int id, string? note = null, CancellationToken ct = default)
        => TransitionStatusAsync(id, StatusSettled, string.IsNullOrWhiteSpace(note) ? "Workflow has been settled" : note, ct);



    private async Task<TransferWorkflowDto> TransitionStatusAsync(int id, int toStatus, string? note, CancellationToken ct = default)
    {
        var workflow = await _transferWorkflowRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Transfer workflow {id} was not found.");

        if (workflow is null)
            throw new InvalidOperationException($"Transfer workflow {id} was not found.");

        if (!IsValidStatusTransition(workflow.Status, toStatus))        
            throw new InvalidOperationException($"Invalid status transition: {workflow.Status} -> {toStatus}.");
        
        if (workflow.Status == toStatus)        
            return _mapper.Map<TransferWorkflowDto>(workflow);
        
        var nowUtc = DateTime.UtcNow;
        var fromStatus = workflow.Status;
        workflow.Status = toStatus;
        workflow.EffectiveAtUtc = nowUtc;
        workflow.UpdatedAtUtc = nowUtc;
        workflow.UpdatedBy = "system"; //todo: get the user from context

        if (toStatus == StatusExecuted)
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
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Note = note,
            UpdatedAtUtc = nowUtc,
            UpdatedBy = "system"  //todo: get the user from context          
        });

        var updated = await _transferWorkflowRepository.UpdateAsync(workflow, ct);
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
