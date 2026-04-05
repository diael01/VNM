
using ConsumptionPolling.Services;
using Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Polling.Services.Auth;

public class ConsumptionPollerFactory : IConsumptionPollerFactory
{
    private readonly IServiceProvider _sp;

    public ConsumptionPollerFactory(IServiceProvider sp)
    {
        _sp = sp;
    }

    public IConsumptionPoller Create(ConsumptionPollingOptions options)
    {
        return options.Protocol.ToLower() switch
        {
            "http" => new HttpConsumptionPoller(
                (HttpClient)_sp.GetService(typeof(HttpClient))!,
                options.HttpEndpoint,
                 _sp.GetRequiredService<IAccessTokenProvider>(),
                    options.Source),             
            // Add more protocols (e.g., TCP, Modbus) as needed
            _ => throw new InvalidOperationException($"Unsupported protocol: {options.Protocol}")
        };
    }
}
