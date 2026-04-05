using InverterPolling.Services;
using Polling.Services.Auth;
using InverterPolling.Services.InverterPoller.Services;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Options;

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
                    _sp.GetRequiredService<IAccessTokenProvider>(),
                    options.Source),
            _ => throw new InvalidOperationException($"Unsupported protocol: {options.Protocol}")
        };
    }
}
