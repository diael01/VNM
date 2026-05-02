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
    Task<TransferWorkflowDto> ApproveAsync(int id, string? note = null);
    Task<TransferWorkflowDto> RejectAsync(int id, string? note = null);
    Task<TransferWorkflowDto> ExecuteAsync(int id, string? note = null);
    Task<TransferWorkflowDto> SettleAsync(int id, string? note = null);
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

    public Task<TransferWorkflowDto> ApproveAsync(int id, string? note = null)
        => TransitionStatusAsync(id, StatusApproved, string.IsNullOrWhiteSpace(note) ? "Workflow has been approved" : note);

    public Task<TransferWorkflowDto> RejectAsync(int id, string? note = null)
        => TransitionStatusAsync(id, StatusRejected, string.IsNullOrWhiteSpace(note) ? "Workflow has been rejected" : note);

    public Task<TransferWorkflowDto> ExecuteAsync(int id, string? note = null)
        => TransitionStatusAsync(id, StatusExecuted, note);

    public Task<TransferWorkflowDto> SettleAsync(int id, string? note = null)
        => TransitionStatusAsync(id, StatusSettled, note);

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
