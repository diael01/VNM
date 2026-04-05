using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class ProviderSettlement
{
    public int Id { get; set; }

    public int AddressId { get; set; }

    public DateTime Day { get; set; }

    public decimal InjectedKwh { get; set; }

    public decimal AcceptedKwh { get; set; }

    public decimal RatePerKwh { get; set; }

    public decimal MonetaryCredit { get; set; }

    public decimal EnergyCreditKwh { get; set; }

    public DateTime ProcessedAtUtc { get; set; }

    public int SettlementMode { get; set; }

    public virtual Address Address { get; set; } = null!;
}
