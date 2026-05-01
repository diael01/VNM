using EnergyManagement.Services.Transfers.Execution;

namespace EnergyManagementWeb.IntegrationTests;

public sealed class TestTransferExecutionAdapter : ITransferExecutionAdapter
{
    public Task<TransferExecutionResult> ExecuteAsync(TransferExecutionRequest request, CancellationToken ct)
    {
        var result = TransferExecutionResult.Succeeded($"ITEST-{request.WorkflowId}-{Guid.NewGuid():N}");
        return Task.FromResult(result);
    }
}
