using Repositories.Models;

namespace EnergyManagement.Services.Providers;

public interface IProviderSettlementService
{
    /// <summary>
    /// Daily/provider reconciliation settlement for a source -> destination pair.
    /// This is NOT tied to one transfer workflow.
    /// Use this only when you intentionally settle a pair outside the workflow button.
    /// </summary>
    Task<ProviderSettlement> ProcessSettlementAsync(
        int sourceAddressId,
        int destinationAddressId,
        DateOnly day,
        CancellationToken ct = default);

    /// <summary>
    /// Settles one already-executed transfer workflow with the provider/grid.
    /// This is the method behind the UI Settle button.
    /// </summary>
    Task<ProviderSettlement> SettleWorkflowAsync(
        int workflowId,
        string? note = null,
        CancellationToken ct = default);
}
