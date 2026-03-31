using EnergyManagement.Services.ModeSwitching;

public interface ISettlementModeResolver
{
    SettlementMode GetCurrentMode();
    ISettlementModeStrategy Resolve(SettlementMode mode);
}