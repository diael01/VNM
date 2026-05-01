
public interface ITransferExecutionService
{
    Task ExecuteAsync(int workflowId, string? executedBy, CancellationToken ct);
}
public interface ITransferExecutionAdapter
{
    Task<TransferExecutionResult> ExecuteAsync(
        TransferExecutionRequest request,
        CancellationToken ct);
}