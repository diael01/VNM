using Repositories.Models;

namespace InverterPolling.Services
{
    /// <summary>
    /// Protocol-agnostic interface for polling inverter data.
    /// Implementations can be HTTP, TCP, Modbus, etc.
    /// </summary>
    public interface IInverterPoller
    {
        /// <summary>
        /// Polls the inverter and returns a reading.
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>An InverterReading or null if polling failed.</returns>
        Task<InverterReading?> PollAsync(CancellationToken ct = default);
    }
}
