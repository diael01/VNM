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

    public int SettlementMode { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
