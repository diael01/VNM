using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using EnergyManagement.Services.Transfers.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Transfers;

public class HttpTransferExecutionAdapterTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSimulatorReturnsSuccess_MapsResult()
    {
        var executedAt = DateTime.UtcNow;
        var handler = new QueueHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TransferExecutionResultDto
                {
                    Success = true,
                    ExternalReference = "SIM-123",
                    ExecutedAtUtc = executedAt
                })
            });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var sut = new HttpTransferExecutionAdapter(
            client,
            Mock.Of<ILogger<HttpTransferExecutionAdapter>>());

        var result = await sut.ExecuteAsync(BuildRequest(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("SIM-123", result.ExternalReference);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(executedAt, result.ExecutedAtUtc);
        Assert.Single(handler.Requests);
        Assert.Equal("/api/simulators/transfer-execution/execute", handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSimulatorReturnsNonSuccess_ReturnsFailed()
    {
        var handler = new QueueHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.BadRequest));

        var sut = new HttpTransferExecutionAdapter(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            Mock.Of<ILogger<HttpTransferExecutionAdapter>>());

        var result = await sut.ExecuteAsync(BuildRequest(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("HTTP 400", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSimulatorReturnsEmptyBody_ThrowsJsonException()
    {
        var handler = new QueueHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty)
            });

        var sut = new HttpTransferExecutionAdapter(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            Mock.Of<ILogger<HttpTransferExecutionAdapter>>());

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() =>
            sut.ExecuteAsync(BuildRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        var handler = new QueueHttpMessageHandler(
            _ => throw new OperationCanceledException("cancelled"));

        var sut = new HttpTransferExecutionAdapter(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            Mock.Of<ILogger<HttpTransferExecutionAdapter>>());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ExecuteAsync(BuildRequest(), cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTransientFailureThenSuccess_RetriesAndSucceeds()
    {
        var attempts = 0;
        var handler = new QueueHttpMessageHandler(
            _ =>
            {
                attempts++;
                if (attempts == 1)
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new TransferExecutionResultDto
                    {
                        Success = true,
                        ExternalReference = "SIM-RETRY",
                        ExecutedAtUtc = DateTime.UtcNow
                    })
                };
            });

        var sut = new HttpTransferExecutionAdapter(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            Mock.Of<ILogger<HttpTransferExecutionAdapter>>());

        var result = await sut.ExecuteAsync(BuildRequest(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("SIM-RETRY", result.ExternalReference);
        Assert.Equal(2, attempts);
    }

    private static TransferExecutionRequest BuildRequest() => new()
    {
        WorkflowId = 10,
        SourceAddressId = 1,
        DestinationAddressId = 2,
        AmountKwh = 12.5m,
        BalanceDay = DateOnly.FromDateTime(DateTime.UtcNow),
        CorrelationId = "corr-1"
    };

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public QueueHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_responder(request));
        }
    }
}