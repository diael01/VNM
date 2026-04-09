using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class TransferWorkflow
{
    public int Id { get; set; }

    public DateTime EffectiveAtUtc { get; set; }

    public DateTime BalanceDayUtc { get; set; }

    public int SourceAddressId { get; set; }

    public int DestinationAddressId { get; set; }

    public decimal SourceSurplusKwhAtWorkflow { get; set; }

    public decimal DestinationDeficitKwhAtWorkflow { get; set; }

    public int TriggerType { get; set; }

    public int Status { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public int AppliedDistributionMode { get; set; }

    public int? TransferRuleId { get; set; }

    public int? Priority { get; set; }

    public decimal? WeightPercent { get; set; }

    public decimal RemainingSourceSurplusKwhAfterWorkflow { get; set; }

    public decimal AmountKwh { get; set; }

    public virtual Address DestinationAddress { get; set; } = null!;

    public virtual Address SourceAddress { get; set; } = null!;

    public virtual TransferRule? TransferRule { get; set; }
}
