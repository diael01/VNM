using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class InverterInfo
{
    public int Id { get; set; }

    public string? Model { get; set; }

    public string? Manufacturer { get; set; }

    public string? SerialNumber { get; set; }

    public virtual ICollection<InverterReading> InverterReadings { get; set; } = new List<InverterReading>();
}
