namespace EnergyManagement.Services.Transfers;

public interface IAvailableBalanceService
{
    Task<AvailableTransferBalanceDto> GetAvailableBalanceAsync(int addressId, DateOnly day, CancellationToken ct = default);
}