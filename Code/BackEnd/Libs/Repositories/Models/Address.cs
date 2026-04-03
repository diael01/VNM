using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class Address
{
    public int Id { get; set; }

    public string Country { get; set; } = null!;

    public string County { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Street { get; set; } = null!;

    public string StreetNumber { get; set; } = null!;

    public string PostalCode { get; set; } = null!;

    public virtual ICollection<ConsumptionReading> ConsumptionReadings { get; set; } = new List<ConsumptionReading>();

    public virtual ICollection<DailyEnergyBalance> DailyEnergyBalances { get; set; } = new List<DailyEnergyBalance>();

    public virtual ICollection<InverterInfo> InverterInfos { get; set; } = new List<InverterInfo>();

    public virtual ICollection<ProviderSettlement> ProviderSettlements { get; set; } = new List<ProviderSettlement>();
}
