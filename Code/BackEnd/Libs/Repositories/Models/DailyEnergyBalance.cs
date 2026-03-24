using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class DailyEnergyBalance
{
    public int Id { get; set; }

    public int? LocationId { get; set; }

    public DateTime? Day { get; set; }

    public decimal? ProducedKwh { get; set; }

    public decimal? ConsumedKwh { get; set; }

    public decimal? SurplusKwh { get; set; }

    public decimal? DeficitKwh { get; set; }

    public DateTime? CalculatedAtUtc { get; set; }

    public string? Status { get; set; }

    public virtual Address? Location { get; set; }
}
