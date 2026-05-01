using Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Models;

namespace EnergyManagement.Services.Transfers.Execution;

public sealed class TransferExecutionService : ITransferExecutionService
{
    private readonly VnmDbContext _db;
    private readonly ITransferExecutionAdapter _adapter;
    private readonly ILogger<TransferExecutionService> _logger;

    public TransferExecutionService(
        VnmDbContext db,
        ITransferExecutionAdapter adapter,
        ILogger<TransferExecutionService> logger)
    {
        _db = db;
        _adapter = adapter;
        _logger = logger;
    }

    public async Task ExecuteAsync(int workflowId, string? executedBy, CancellationToken ct)
    {
        var workflow = await _db.TransferWorkflows
            .FirstOrDefaultAsync(x => x.Id == workflowId, ct);

        if (workflow is null)
            throw new InvalidOperationException($"Transfer workflow {workflowId} was not found.");

        if (workflow.Status != (int)TransferStatus.Approved)
            throw new InvalidOperationException(
                $"Workflow {workflowId} must be Approved before execution. Current status: {workflow.Status}");

        var request = new TransferExecutionRequest
        {
            WorkflowId = workflow.Id,
            SourceAddressId = workflow.SourceAddressId,
            DestinationAddressId = workflow.DestinationAddressId,
            AmountKwh = workflow.AmountKwh,
            BalanceDay = DateOnly.FromDateTime(workflow.BalanceDayUtc),
            RequestedAtUtc = DateTime.UtcNow,
            RequestedBy = executedBy,
            Notes = "Manual execution from VNM workflow."
        };

        _logger.LogInformation(
            "Executing transfer workflow {WorkflowId}. Source={Source}, Destination={Destination}, AmountKwh={AmountKwh}",
            workflow.Id,
            workflow.SourceAddressId,
            workflow.DestinationAddressId,
            workflow.AmountKwh);

        var result = await _adapter.ExecuteAsync(request, ct);

        var oldStatus = workflow.Status;

        if (!result.Success)
        {
            workflow.Status = (int)TransferStatus.Failed;
            workflow.UpdatedAtUtc = DateTime.UtcNow;
            workflow.UpdatedBy = executedBy;

            _db.TransferWorkflowStatusHistory.Add(new TransferWorkflowStatusHistory
            {
                TransferWorkflowId = workflow.Id,
                FromStatus = oldStatus,
                ToStatus = workflow.Status,
                UpdatedAtUtc = DateTime.UtcNow,
                UpdatedBy = executedBy ?? "system",
                Note = result.ErrorMessage ?? "Transfer execution failed."
            });

            await _db.SaveChangesAsync(ct);
            return;
        }

        _db.TransferLedgerEntries.Add(new TransferLedgerEntry
        {
            TransferWorkflowId = workflow.Id,
            SourceAddressId = workflow.SourceAddressId,
            DestinationAddressId = workflow.DestinationAddressId,
            BalanceDay = DateOnly.FromDateTime(workflow.BalanceDayUtc),
            AmountKwh = workflow.AmountKwh,
            ExecutedAtUtc = result.ExecutedAtUtc,
            ExecutionReference = result.ExternalReference ?? Guid.NewGuid().ToString("N"),
            Notes = "Executed by transfer simulator."
        });

        workflow.Status = (int)TransferStatus.Executed;
        workflow.UpdatedAtUtc = DateTime.UtcNow;
        workflow.UpdatedBy = executedBy;

        _db.TransferWorkflowStatusHistory.Add(new TransferWorkflowStatusHistory
        {
            TransferWorkflowId = workflow.Id,
            FromStatus = oldStatus,
            ToStatus = workflow.Status,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedBy = executedBy ?? "system",
            Note = $"Executed. ExternalReference={result.ExternalReference}"
        });

        await _db.SaveChangesAsync(ct);
    }
}