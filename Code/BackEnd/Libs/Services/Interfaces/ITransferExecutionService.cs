
public interface ITransferExecutionService
{
    Task ExecuteAsync(int workflowId, string? executedBy, string? note, CancellationToken ct);
}
public interface ITransferExecutionAdapter
{
    Task<TransferExecutionResult> ExecuteAsync(
        TransferExecutionRequest request,
        CancellationToken ct);
}