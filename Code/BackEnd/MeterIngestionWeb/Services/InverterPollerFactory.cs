using Microsoft.Extensions.DependencyInjection;
using System;
using InverterPolling.Services;

public class InverterPollerFactory : IInverterPollerFactory
{
    private readonly IServiceProvider _sp;

    public InverterPollerFactory(IServiceProvider sp)
    {
        _sp = sp;
    }

    public IInverterPoller Create(InverterPollingOptions options)
    {
        return options.Protocol.ToLower() switch
        {
            "tcp" => new TcpInverterPoller(options.TcpHost, options.TcpPort),
            "http" =>
                new HttpInverterPoller(
                    _sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
                    options.HttpEndpoint,
                    options.Source),
            _ => throw new InvalidOperationException($"Unsupported protocol: {options.Protocol}")
        };
    }
}
