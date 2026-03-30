using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class ProviderSettlement
{
    public int Id { get; set; }

    public int? LocationId { get; set; }

    public DateTime? Day { get; set; }

    public decimal? InjectedKwh { get; set; }

    public decimal? AcceptedKwh { get; set; }

    public decimal? RatePerKwh { get; set; }

    public decimal? MonetaryCredit { get; set; }

    public decimal? EnergyCreditKwh { get; set; }

    public SettlementMode SettlementMode { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public virtual Address? Location { get; set; }
}
