using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class SourceTransferPolicy : AuditableEntity
{
    public int Id { get; set; }

    public int SourceAddressId { get; set; }

    public int DistributionMode { get; set; }

    public bool IsEnabled { get; set; }

    public virtual Address SourceAddress { get; set; } = null!;

    public virtual ICollection<DestinationTransferRule> DestinationTransferRules { get; set; }
        = new List<DestinationTransferRule>();

    public virtual ICollection<SourceTransferSchedule> SourceTransferSchedules { get; set; }
        = new List<SourceTransferSchedule>();
}


//IsEnabled Meaning
/* 1. SourceTransferPolicy.IsEnabled

This is the big switch.

Meaning:

Is this source policy active at all?

If false:

ignore the whole policy
ignore all child destination rules
ignore all schedules under it

This is the parent on/off. */