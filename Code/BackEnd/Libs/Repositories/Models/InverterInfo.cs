using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class InverterInfo
{
    public int Id { get; set; }

    public string Model { get; set; } = null!;

    public string Manufacturer { get; set; } = null!;

    public string SerialNumber { get; set; } = null!;

    public int AddressId { get; set; }

    public virtual Address Address { get; set; } = null!;

    public virtual ICollection<ConsumptionReading> ConsumptionReadings { get; set; } = new List<ConsumptionReading>();

    public virtual ICollection<DailyEnergyBalance> DailyEnergyBalances { get; set; } = new List<DailyEnergyBalance>();

    public virtual ICollection<InverterReading> InverterReadings { get; set; } = new List<InverterReading>();
}
