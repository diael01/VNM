using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class ConsumptionReading
{
    public int Id { get; set; }

    public int? LocationId { get; set; }

    public DateTime? Timestamp { get; set; }

    public int? Power { get; set; }

    public string? Source { get; set; }

    public virtual Address? Location { get; set; }
}
