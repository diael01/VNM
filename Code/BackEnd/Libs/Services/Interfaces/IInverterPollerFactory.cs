using Infrastructure.Options;
using InverterPolling.Services;
public interface IInverterPollerFactory
{
    IInverterPoller Create(InverterPollingOptions options);
}
