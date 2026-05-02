using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Infrastructure.DTOs;

public class TransferWorkflowDto
{
    [BindNever]
    public int Id { get; set; }

    public DateTime EffectiveAtUtc { get; set; }
    public DateTime BalanceDayUtc { get; set; }
    public int SourceAddressId { get; set; }
    public int DestinationAddressId { get; set; }
    public decimal SourceSurplusKwhAtWorkflow { get; set; }
    public decimal DestinationDeficitKwhAtWorkflow { get; set; }
    public decimal? RemainingSourceSurplusKwhAfterWorkflow { get; set; }
    public decimal? RemainingDestinationDeficitKwhAfterWorkflow { get; set; }
    public decimal AmountKwh { get; set; }
    public int TriggerType { get; set; }
    public int Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int AppliedDistributionMode { get; set; }
    public int? DestinationTransferRuleId { get; set; }
    public int? Priority { get; set; }
    public decimal? WeightPercent { get; set; }
}
