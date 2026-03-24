using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class InverterReading
{
    public int Id { get; set; }

    public int? LocationId { get; set; }

    public DateTime? Timestamp { get; set; }

    public int? Power { get; set; }

    public int? Voltage { get; set; }

    public int? Current { get; set; }

    public string? Source { get; set; }

    public int? InverterId { get; set; }

    public virtual InverterInfo? Inverter { get; set; }

    public virtual Address? Location { get; set; }
}
