using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class DailyEnergyBalance
{
    public int Id { get; set; }
   
    public DateTime? Day { get; set; }

    public decimal? ProducedKwh { get; set; }

    public decimal? ConsumedKwh { get; set; }

    public decimal? SurplusKwh { get; set; }

    public decimal? DeficitKwh { get; set; }

    public DateTime? CalculatedAtUtc { get; set; }

    public string? Status { get; set; }

    public decimal? NetKwh { get; set; }

     public decimal? NetPerAddressKwh { get; set; }
   
    public int InverterInfoId { get; set; }    

    public virtual InverterInfo InverterInfo { get; set; }

    public int AddressId { get; set; }    

    public virtual Address Address { get; set; }
}
