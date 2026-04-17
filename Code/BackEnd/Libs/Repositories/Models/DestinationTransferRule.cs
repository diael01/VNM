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

//Is Enabled meaning:
/* 2. DestinationTransferRule.IsEnabled

This is a child-level switch.

Meaning:

Under this source policy, is this specific destination participating?

Example:

Farm policy is enabled
Hospital rule enabled
School rule disabled temporarily

This is useful if:
    
a destination is under maintenance
a contract is paused
you want to test with only one destination without deleting the row 

Destinations section
Enabled per destination
*/