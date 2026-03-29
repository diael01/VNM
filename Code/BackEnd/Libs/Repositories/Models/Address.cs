using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class Address
{
    public int Id { get; set; }

    public string? Country { get; set; }

    public string? County { get; set; }

    public string? City { get; set; }

    public string? Street { get; set; }

    public string? StreetNumber { get; set; }

    public string? PostalCode { get; set; }


    public virtual ICollection<ConsumptionReading> ConsumptionReadings { get; set; } = new List<ConsumptionReading>();

    public virtual ICollection<DailyEnergyBalance> DailyEnergyBalances { get; set; } = new List<DailyEnergyBalance>();

    public virtual ICollection<InverterReading> InverterReadings { get; set; } = new List<InverterReading>();

    public virtual ICollection<ProviderSettlement> ProviderSettlements { get; set; } = new List<ProviderSettlement>();
}
