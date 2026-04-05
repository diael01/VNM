 public class TransferImpactDto
{
    public int DestinationAddressId { get; set; }
    public DateOnly Day { get; set; }

    public decimal OriginalDeficitKwh { get; set; }
    public decimal CoveredByTransferKwh { get; set; }
    public decimal RemainingDeficitKwh { get; set; }

    public decimal OriginalCost { get; set; }
    public decimal CoveredValue { get; set; }
    public decimal RemainingCost { get; set; }
}