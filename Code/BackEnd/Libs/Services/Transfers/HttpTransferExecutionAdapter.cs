using System.Net.Http.Json;

namespace EnergyManagement.Services.Transfers.Execution;

public sealed class HttpTransferExecutionAdapter : ITransferExecutionAdapter
{
    private readonly HttpClient _httpClient;

    public HttpTransferExecutionAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
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

        var response = await _httpClient.PostAsJsonAsync(
            "api/simulators/transfer-execution/execute",
            dto,
            ct);

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