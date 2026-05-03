using Repositories.Models;
public interface ITransferWorkflowScheduledService
{
    Task<IReadOnlyList<TransferWorkflow>> RunAutomaticWorkflowAsync(
        DateOnly day,
        CancellationToken ct = default);

    Task<IReadOnlyList<TransferWorkflow>> RunAutomaticWorkflowForSourceAsync(
        int sourceAddressId,
        DateOnly day,
        CancellationToken ct = default);

}
