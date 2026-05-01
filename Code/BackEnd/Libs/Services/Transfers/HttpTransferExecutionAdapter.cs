using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace EnergyManagement.Services.Transfers.Execution;

public sealed class HttpTransferExecutionAdapter : ITransferExecutionAdapter
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan CircuitBreakDuration = TimeSpan.FromSeconds(30);

    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpTransferExecutionAdapter> _logger;

    public HttpTransferExecutionAdapter(
        HttpClient httpClient,
        ILogger<HttpTransferExecutionAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TransferExecutionResult> ExecuteAsync(
        TransferExecutionRequest request,
        CancellationToken ct)
    {
        var dto = new TransferExecutionSimulatorRequestDto
        {
            WorkflowId = request.WorkflowId,
            SourceAddressId = request.SourceAddressId,
            DestinationAddressId = request.DestinationAddressId,
            AmountKwh = request.AmountKwh,
            BalanceDay = request.BalanceDay,
            CorrelationId = request.CorrelationId
        };

        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult(response =>
                response.StatusCode == HttpStatusCode.RequestTimeout
                || (int)response.StatusCode == 429
                || (int)response.StatusCode >= 500)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds((Math.Pow(2, retryAttempt) * 200) + Random.Shared.Next(0, 150)),
                onRetry: (outcome, delay, retryAttempt, _) =>
                {
                    var error = outcome.Exception?.Message
                        ?? $"HTTP {(int)outcome.Result.StatusCode}";

                    _logger.LogWarning(
                        "Retrying transfer execution simulator call for workflow {WorkflowId}. Attempt={RetryAttempt}, DelayMs={DelayMs}, Cause={Cause}",
                        request.WorkflowId,
                        retryAttempt,
                        (int)delay.TotalMilliseconds,
                        error);
                });

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(RequestTimeout);

        var circuitBreakerPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult(response =>
                response.StatusCode == HttpStatusCode.RequestTimeout
                || (int)response.StatusCode == 429
                || (int)response.StatusCode >= 500)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: CircuitBreakDuration,
                onBreak: (outcome, breakDelay) =>
                {
                    var error = outcome.Exception?.Message
                        ?? $"HTTP {(int)outcome.Result.StatusCode}";

                    _logger.LogWarning(
                        "Transfer execution circuit breaker opened for {BreakSeconds}s. Cause={Cause}",
                        (int)breakDelay.TotalSeconds,
                        error);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Transfer execution circuit breaker reset.");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Transfer execution circuit breaker is half-open; next call is a probe.");
                });

        var resiliencePolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);

        HttpResponseMessage response;
        try
        {
            response = await resiliencePolicy.ExecuteAsync(token =>
                    _httpClient.PostAsJsonAsync(
                        "api/simulators/transfer-execution/execute",
                        dto,
                        token),
                ct);
        }
        catch (BrokenCircuitException)
        {
            return TransferExecutionResult.Failed("Simulator circuit breaker is open. Please retry later.");
        }
        catch (TimeoutRejectedException)
        {
            return TransferExecutionResult.Failed("Simulator call timed out.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return TransferExecutionResult.Failed($"Simulator call failed after retries: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            return TransferExecutionResult.Failed(
                $"Simulator returned HTTP {(int)response.StatusCode}.");
        }

        var resultDto = await response.Content
            .ReadFromJsonAsync<TransferExecutionResultDto>(cancellationToken: ct);

        if (resultDto is null)
            return TransferExecutionResult.Failed("Simulator returned empty response.");

        return new TransferExecutionResult
        {
            Success = resultDto.Success,
            ExternalReference = resultDto.ExternalReference,
            ErrorMessage = resultDto.ErrorMessage,
            ExecutedAtUtc = resultDto.ExecutedAtUtc
        };
    }
}