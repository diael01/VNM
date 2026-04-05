public class TransferAllocation
{
    public int Id { get; set; }

    public DateTime Day { get; set; }

    public int SourceAddressId { get; set; }
    public int DestinationAddressId { get; set; }

    public decimal RequestedKwh { get; set; }
    public decimal AllocatedKwh { get; set; }

    public string Mode { get; set; } = "Auto"; // Auto / Manual
    public string Status { get; set; } = "Planned"; // Planned / Executed / Settled

    public int? Priority { get; set; }
    public decimal? WeightPercent { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public int? TransferRuleId { get; set; }
}