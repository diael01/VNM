
using System;

using Simulators.Models;

namespace Simulators.Simulation;

public class SolarProductionSimulator
{
    public void Tick(InverterState state)
    {
        var hour = DateTime.Now.Hour;

        if (hour < 6 || hour > 20)
        {
            state.CurrentPowerWatts = 0;
            state.Status = InverterStatus.Standby;
        }
        else
        {
            var peakWatts = 2200m; // 2.2 kW inverter
            var factor = Math.Sin((hour - 6) / 14.0 * Math.PI);

            state.CurrentPowerWatts = peakWatts * (decimal)factor;
            state.Status = InverterStatus.Producing;

            state.DailyEnergyKWh += state.CurrentPowerWatts / 1000m / 60m;
            state.TotalEnergyKWh += state.CurrentPowerWatts / 1000m / 60m;
        }

        state.Timestamp = DateTime.UtcNow;
    }
}


