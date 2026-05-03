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

        var executionRequestedAtUtc = DateTime.UtcNow;
        var executionDayStartUtc = executionRequestedAtUtc.Date;
        var executionDayEndUtc = executionDayStartUtc.AddDays(1);

        var currentPositions = await _db.DailyEnergyBalances
            .Where(x =>
                x.Day >= executionDayStartUtc &&
                x.Day < executionDayEndUtc &&
                (x.AddressId == workflow.SourceAddressId || x.AddressId == workflow.DestinationAddressId))
            .Select(x => new
            {
                x.AddressId,
                x.SurplusKwh,
                x.DeficitKwh
            })
            .ToListAsync(ct);

        var currentSourceSurplusKwh = currentPositions
            .Where(x => x.AddressId == workflow.SourceAddressId)
            .Select(x => x.SurplusKwh)
            .FirstOrDefault();

        var currentDestinationDeficitKwh = currentPositions
            .Where(x => x.AddressId == workflow.DestinationAddressId)
            .Select(x => x.DeficitKwh)
            .FirstOrDefault();

        var executableKwh = decimal.Round(
            Math.Min(
                workflow.AmountKwh,
                Math.Min(currentSourceSurplusKwh, currentDestinationDeficitKwh)),
            4);

        if (executableKwh <= 0)
            throw new InvalidOperationException(
                $"Workflow {workflowId} cannot be executed because the current transferable amount is 0 kWh.");

        var request = new TransferExecutionRequest
        {
            WorkflowId = workflow.Id,
            SourceAddressId = workflow.SourceAddressId,
            DestinationAddressId = workflow.DestinationAddressId,
            AmountKwh = executableKwh,
            BalanceDay = DateOnly.FromDateTime(executionDayStartUtc),
            RequestedAtUtc = executionRequestedAtUtc,
            RequestedBy = executedBy,
            Notes = executionNote
        };

        _logger.LogInformation(
            "Executing transfer workflow {WorkflowId}. Source={Source}, Destination={Destination}, ApprovedAmountKwh={ApprovedAmountKwh}, ExecutableAmountKwh={ExecutableAmountKwh}, CurrentSourceSurplusKwh={CurrentSourceSurplusKwh}, CurrentDestinationDeficitKwh={CurrentDestinationDeficitKwh}",
            workflow.Id,
            workflow.SourceAddressId,
            workflow.DestinationAddressId,
            workflow.AmountKwh,
            executableKwh,
            currentSourceSurplusKwh,
            currentDestinationDeficitKwh);

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
            BalanceDay = DateOnly.FromDateTime(executionDayStartUtc),
            AmountKwh = executableKwh,
            ExecutedAtUtc = result.ExecutedAtUtc,
            ExecutionReference = result.ExternalReference ?? Guid.NewGuid().ToString("N"),
            Notes = executionNote
        });

        workflow.TransferStatusEnum = TransferStatus.Executed;
        workflow.EffectiveAtUtc = DateTime.UtcNow;
        workflow.AmountAtExecutionKwh = executableKwh;
        workflow.SourceSurplusKwhAtExecution = decimal.Round(currentSourceSurplusKwh, 4);
        workflow.DestinationDeficitKwhAtExecution = decimal.Round(currentDestinationDeficitKwh, 4);
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