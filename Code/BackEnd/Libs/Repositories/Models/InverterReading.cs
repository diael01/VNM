using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class InverterReading
{
    public int Id { get; set; }

    public int? LocationId { get; set; }

    public DateTime? Timestamp { get; set; }

    public decimal? Power { get; set; }

    public decimal? Voltage { get; set; }

    public decimal? Current { get; set; }

    public string? Source { get; set; }

    public int? InverterId { get; set; }

    public virtual InverterInfo? Inverter { get; set; }

    public virtual Address? Location { get; set; }
}
