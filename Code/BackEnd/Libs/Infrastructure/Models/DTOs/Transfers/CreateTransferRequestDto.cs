
  public class CreateTransferRequestDto
    {
        public int SourceAddressId { get; set; }
        public int DestinationAddressId { get; set; }
        public DateOnly Day { get; set; }
        public decimal Amount { get; set; }
        public int Priority { get; set; }
    }