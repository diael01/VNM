/* public class CreateTransferRequestDto
{
    public int FromAddressId { get; set; }
    public int ToAddressId { get; set; }
    public DateOnly Day { get; set; }
    public string SettlementMode { get; set; } = "Money";
    public decimal KwhAmount { get; set; }
    public decimal MoneyAmount { get; set; }
} */

  public class CreateTransferRequestDto
    {
        public int SourceAddressId { get; set; }
        public int DestinationAddressId { get; set; }
        public DateOnly Day { get; set; }
        public decimal Amount { get; set; }
    }