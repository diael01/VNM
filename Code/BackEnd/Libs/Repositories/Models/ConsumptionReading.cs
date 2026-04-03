using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class ConsumptionReading
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    public decimal Power { get; set; }

    public string Source { get; set; } = null!;

    public int AddressId { get; set; }

    public virtual Address Address { get; set; } = null!;
}
