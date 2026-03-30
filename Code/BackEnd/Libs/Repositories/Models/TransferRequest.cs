
public class TransferRequest
{
    public int Id { get; set; }
    public int SourceAddressId { get; set; }
    public int DestinationAddressId { get; set; }
    public DateOnly Day { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public SettlementMode SettlementMode { get; set; } //money or energy
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
} 