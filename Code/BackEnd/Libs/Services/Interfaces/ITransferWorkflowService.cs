using Repositories.Models;
public interface ITransferWorkflowService
{
    Task<IReadOnlyList<TransferWorkflow>> RunAutomaticWorkflowAsync(
        DateOnly day,
        CancellationToken ct = default);

    Task<IReadOnlyList<TransferWorkflow>> RunAutomaticWorkflowForSourceAsync(
        int sourceAddressId,
        DateOnly day,
        CancellationToken ct = default);

    Task<IReadOnlyList<TransferWorkflow>> ExecuteManualTransferAsync(
        ManualTransferRequest request,
        CancellationToken ct = default);
}
