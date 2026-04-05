namespace EnergyManagement.Services.Transfers;

public class AddressTransferPosition
{
    public int AddressId { get; set; }
    public decimal DailySurplusKwh { get; set; }
    public decimal DailyDeficitKwh { get; set; }

    public decimal AlreadyTransferredOutKwh { get; set; }
    public decimal AlreadyTransferredInKwh { get; set; }

    public decimal RemainingSurplusKwh =>
        Math.Max(0m, DailySurplusKwh - AlreadyTransferredOutKwh);

    public decimal RemainingDeficitKwh =>
        Math.Max(0m, DailyDeficitKwh - AlreadyTransferredInKwh);
}