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

    public async Task ExecuteAsync(int workflowId, string? executedBy, string? note, CancellationToken ct)
    {
        var workflow = await _db.TransferWorkflows
            .FirstOrDefaultAsync(x => x.Id == workflowId, ct);

        if (workflow is null)
            throw new InvalidOperationException($"Transfer workflow {workflowId} was not found.");

        if (workflow.TransferStatusEnum != TransferStatus.Approved)
            throw new InvalidOperationException(
                $"Workflow {workflowId} must be Approved before execution. Current status: {workflow.Status}");

        var userNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        var executionNote = userNote ?? "Manual execution from VNM workflow.";

        var request = new TransferExecutionRequest
        {
            WorkflowId = workflow.Id,
            SourceAddressId = workflow.SourceAddressId,
            DestinationAddressId = workflow.DestinationAddressId,
            AmountKwh = workflow.AmountKwh,
            BalanceDay = DateOnly.FromDateTime(workflow.BalanceDayUtc),
            RequestedAtUtc = DateTime.UtcNow,
            RequestedBy = executedBy,
            Notes = executionNote
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
            workflow.TransferStatusEnum = TransferStatus.Failed;
            workflow.UpdatedAtUtc = DateTime.UtcNow;
            workflow.UpdatedBy = executedBy;

            _db.TransferWorkflowStatusHistory.Add(new TransferWorkflowStatusHistory
            {
                TransferWorkflowId = workflow.Id,
                FromStatus = oldStatus,
                ToStatus = workflow.Status,
                UpdatedAtUtc = DateTime.UtcNow,
                UpdatedBy = executedBy ?? "system",
                Note = BuildFailureHistoryNote(result.ErrorMessage, userNote)
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
            Notes = executionNote
        });

        workflow.TransferStatusEnum = TransferStatus.Executed;
        workflow.EffectiveAtUtc = DateTime.UtcNow;
        workflow.RemainingSourceSurplusKwhAfterWorkflow = decimal.Round(
            Math.Max(0m, workflow.SourceSurplusKwhAtWorkflow - workflow.AmountKwh),
            4);
        workflow.RemainingDestinationDeficitKwhAfterWorkflow = decimal.Round(
            Math.Max(0m, workflow.DestinationDeficitKwhAtWorkflow - workflow.AmountKwh),
            4);
        workflow.UpdatedAtUtc = DateTime.UtcNow;
        workflow.UpdatedBy = executedBy;

        _db.TransferWorkflowStatusHistory.Add(new TransferWorkflowStatusHistory
        {
            TransferWorkflowId = workflow.Id,
            FromStatus = oldStatus,
            ToStatus = workflow.Status,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedBy = executedBy ?? "system",
            Note = BuildSuccessHistoryNote(result.ExternalReference, userNote)
        });

        await _db.SaveChangesAsync(ct);
    }

    private static string BuildFailureHistoryNote(string? errorMessage, string? userNote)
    {
        var failure = string.IsNullOrWhiteSpace(errorMessage)
            ? "Transfer execution failed."
            : $"Transfer execution failed. Error={errorMessage}";

        return string.IsNullOrWhiteSpace(userNote)
            ? failure
            : $"{failure} UserNote={userNote}";
    }

    private static string BuildSuccessHistoryNote(string? externalReference, string? userNote)
    {
        var note = string.IsNullOrWhiteSpace(externalReference)
            ? "Executed."
            : $"Executed. ExternalReference={externalReference}";

        return string.IsNullOrWhiteSpace(userNote)
            ? note
            : $"{note} UserNote={userNote}";
    }
}