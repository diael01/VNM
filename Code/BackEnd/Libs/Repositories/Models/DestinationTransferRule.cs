using System;

namespace Repositories.Models;

public partial class DestinationTransferRule : AuditableEntity
{
    public int Id { get; set; }

    public int SourceTransferPolicyId { get; set; }

    public int DistributionMode { get; set; }

    public int DestinationAddressId { get; set; }

    public bool IsEnabled { get; set; }

    public int Priority { get; set; } //Priority is only the order in which destinations are served when DistributionMode = Priority.

    public decimal? WeightPercent { get; set; } //WeightPercent is used only when DistributionMode = Weighted.

    public decimal? MaxDailyKwh { get; set; } //this cap can be used regardless of distribution mode.

    public virtual SourceTransferPolicy SourceTransferPolicy { get; set; } = null!;

    public virtual Address DestinationAddress { get; set; } = null!;
}