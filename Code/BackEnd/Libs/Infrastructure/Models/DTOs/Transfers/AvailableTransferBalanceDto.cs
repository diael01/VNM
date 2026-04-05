
public class AvailableTransferBalanceDto
{
    public int AddressId { get; set; }
    public DateOnly Day { get; set; }
    public string SettlementMode { get; set; } = "Money";
    public decimal AvailableKwh { get; set; }
    public decimal AvailableMoney { get; set; }
}