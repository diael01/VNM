using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class TransferRequest
{
    public int Id { get; set; }

    public int SourceAddressId { get; set; }

    public int DestinationAddressId { get; set; }

    public DateOnly Day { get; set; }

    public decimal RequestedAmount { get; set; }

    public decimal ActualAmount { get; set; }

    public SettlementMode SettlementMode { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
}
