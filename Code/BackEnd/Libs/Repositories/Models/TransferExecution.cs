using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class TransferExecution
{
    public int Id { get; set; }

    public DateTime EffectiveAtUtc { get; set; }

    public DateTime BalanceDayUtc { get; set; }

    public int SourceAddressId { get; set; }

    public int DestinationAddressId { get; set; }

    public decimal RequestedKwh { get; set; }

    public decimal AllocatedKwh { get; set; }

    public int TriggerType { get; set; }

    public int Status { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public int AppliedDistributionMode { get; set; }

    public int? TransferRuleId { get; set; }

    public virtual Address DestinationAddress { get; set; } = null!;

    public virtual Address SourceAddress { get; set; } = null!;

    public virtual TransferRule? TransferRule { get; set; }
}
