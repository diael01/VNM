using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;


namespace EventBusClient;
public class EventBusHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Here you can test connection to RabbitMQ if you want
        return Task.FromResult(HealthCheckResult.Healthy("EventBus is OK"));
    }
}
