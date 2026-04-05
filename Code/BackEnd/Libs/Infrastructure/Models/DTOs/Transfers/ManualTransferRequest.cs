public class ManualTransferRequest
{
    public DateOnly Day { get; set; }

    public int SourceAddressId { get; set; }

    public List<ManualTransferTarget> Targets { get; set; } = new();

    public string Mode { get; set; } = "Exact"; // Exact / Proportional / UseRules

    public string Notes { get; set; }
}