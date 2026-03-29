using System.Threading;
using System.Threading.Tasks;
using Repositories.Models;

namespace ConsumptionPolling.Services
{
    /// <summary>
    /// Protocol-agnostic interface for polling consumption meter data.
    /// </summary>
    public interface IConsumptionPoller
    {
        /// <summary>
        /// Polls the meter and returns a reading.
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>A ConsumptionReading or null if polling failed.</returns>
        Task<ConsumptionReading?> PollAsync(CancellationToken ct = default);
    }
}
