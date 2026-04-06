using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class InverterReading
{
    public int Id { get; set; }

    public int InverterInfoId { get; set; }

    public DateTime Timestamp { get; set; }

    public decimal Power { get; set; }

    public decimal Voltage { get; set; }

    public decimal Current { get; set; }

    public string Source { get; set; } = null!;

    public int AddressId { get; set; }

    public virtual Address Address { get; set; } = null!;

    public virtual InverterInfo InverterInfo { get; set; } = null!;
}
