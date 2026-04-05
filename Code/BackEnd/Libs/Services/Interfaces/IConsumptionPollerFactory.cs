using ConsumptionPolling.Services;
using Infrastructure.Options;

public interface IConsumptionPollerFactory
{
    IConsumptionPoller Create(ConsumptionPollingOptions options);
}
