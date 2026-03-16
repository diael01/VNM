using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class InverterReading
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    public int Power { get; set; }

    public int Voltage { get; set; }

    public int Current { get; set; }

    public string Source { get; set; } = null!;
}
