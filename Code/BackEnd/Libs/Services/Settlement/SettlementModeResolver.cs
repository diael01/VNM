using EnergyManagement.Services.ModeSwitching;
using Infrastructure.Enums;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EnergyManagement.Services.ModeSwitching;

public class SettlementModeResolver : ISettlementModeResolver
{
    private readonly IEnumerable<ISettlementModeStrategy> _strategies;
    private readonly SettlementModeOptions _options;

    public SettlementModeResolver(
        IEnumerable<ISettlementModeStrategy> strategies,
        IOptions<SettlementModeOptions> options)
    {
        _strategies = strategies;
        _options = options.Value;
    }

    public ProviderSettlementMode GetCurrentMode()
    {
        if (Enum.TryParse<ProviderSettlementMode>(_options.CurrentMode, true, out var mode))
            return mode;

        return ProviderSettlementMode.Money;
    }

    public ISettlementModeStrategy Resolve(ProviderSettlementMode mode)
    {
        return _strategies.First(x => x.SettlementMode == mode);
    }
}

