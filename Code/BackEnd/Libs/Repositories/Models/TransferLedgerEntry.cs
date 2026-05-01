namespace Repositories.Models;
public class TransferLedgerEntry: AuditableEntity
{
    public int Id { get; set; }

    public int TransferWorkflowId { get; set; }
    public TransferWorkflow TransferWorkflow { get; set; } = null!;

    public int SourceAddressId { get; set; }
    public int DestinationAddressId { get; set; }

    public DateOnly BalanceDay { get; set; }

    public decimal AmountKwh { get; set; }

    public DateTime ExecutedAtUtc { get; set; }

    public string ExecutionReference { get; set; } = Guid.NewGuid().ToString("N");

    public string? Notes { get; set; }
}