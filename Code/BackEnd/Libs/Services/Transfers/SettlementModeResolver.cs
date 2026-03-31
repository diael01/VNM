using EnergyManagement.Services.ModeSwitching;
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

    public SettlementMode GetCurrentMode()
    {
        if (Enum.TryParse<SettlementMode>(_options.CurrentMode, true, out var mode))
            return mode;

        return SettlementMode.Money;
    }

    public ISettlementModeStrategy Resolve(SettlementMode mode)
    {
        return _strategies.First(x => x.SettlementMode == mode);
    }
}

