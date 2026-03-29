using ConsumptionPolling.Services;

public interface IConsumptionPollerFactory
{
    IConsumptionPoller Create(ConsumptionPollingOptions options);
}
