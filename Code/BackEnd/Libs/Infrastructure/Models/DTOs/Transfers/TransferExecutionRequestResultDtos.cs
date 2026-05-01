//todo: later to move into sep files - if apply

public sealed class TransferExecutionSimulatorRequestDto
{
    public int WorkflowId { get; set; }

    public int SourceAddressId { get; set; }

    public int DestinationAddressId { get; set; }

    public decimal AmountKwh { get; set; }

    public DateOnly BalanceDay { get; set; }

    public string? CorrelationId { get; set; }
}

public sealed class TransferExecutionResultDto
{
    public bool Success { get; set; }

    public string? ExternalReference { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime ExecutedAtUtc { get; set; }
}

public sealed class TransferExecutionResult
{
    public bool Success { get; set; }
    public string? ExternalReference { get; set; }
    public string? ErrorMessage { get; set; }
  public DateTime ExecutedAtUtc { get; init; } = DateTime.UtcNow;

    public static TransferExecutionResult Succeeded(string? externalReference = null)
    {
        return new TransferExecutionResult
        {
            Success = true,
            ExternalReference = externalReference ?? $"SIM-{Guid.NewGuid():N}",
            ExecutedAtUtc = DateTime.UtcNow
        };
    }

    public static TransferExecutionResult Failed(string errorMessage)
    {
        return new TransferExecutionResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ExecutedAtUtc = DateTime.UtcNow
        };
    }
}

public class TransferExecutionRequest
{
    public int WorkflowId { get; set; }

    public int SourceAddressId { get; set; }
    public int DestinationAddressId { get; set; }

    public decimal AmountKwh { get; set; }

    public DateOnly BalanceDay { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");

    public string? RequestedBy { get; set; }

    public string? Notes { get; set; }
} 