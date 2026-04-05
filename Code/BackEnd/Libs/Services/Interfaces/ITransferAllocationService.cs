using Repositories.Models;
public interface ITransferAllocationService
{
    Task<IReadOnlyList<TransferExecution>> RunAutomaticAllocationAsync(
        DateOnly day,
        CancellationToken ct = default);

    Task<IReadOnlyList<TransferExecution>> RunAutomaticAllocationForSourceAsync(
        int sourceAddressId,
        DateOnly day,
        CancellationToken ct = default);

    Task<IReadOnlyList<TransferExecution>> ExecuteManualTransferAsync(
        ManualTransferRequest request,
        CancellationToken ct = default);
}