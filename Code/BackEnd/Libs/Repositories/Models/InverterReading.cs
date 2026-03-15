using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class InverterReading
{
    public int Id { get; set; }

    public DateTime TimestampUtc { get; set; }

    public int PowerW { get; set; }

    public int VoltageV { get; set; }

    public int CurrentA { get; set; }

    public string Source { get; set; } = null!;
}
