using EnergyManagement.Services.ModeSwitching;
using Infrastructure.Enums;

public interface ISettlementModeResolver
{
    ProviderSettlementMode GetCurrentMode();
    ISettlementModeStrategy Resolve(ProviderSettlementMode mode);
}