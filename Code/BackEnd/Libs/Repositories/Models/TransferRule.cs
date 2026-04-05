using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class TransferRule
{
    public int Id { get; set; }

    public int SourceAddressId { get; set; }

    public int DestinationAddressId { get; set; }

    public bool IsEnabled { get; set; }

    public int Priority { get; set; }

    public decimal? WeightPercent { get; set; }

    public decimal? MaxDailyKwh { get; set; }

    public int DistributionMode { get; set; }

    public virtual Address DestinationAddress { get; set; } = null!;

    public virtual Address SourceAddress { get; set; } = null!;

    public virtual ICollection<TransferExecution> TransferExecutions { get; set; } = new List<TransferExecution>();
}
