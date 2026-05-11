using System;
using System.Collections.Generic;

namespace Repositories.Models;

public partial class ProviderSettlement : AuditableEntity
{
    public int Id { get; set; }

    public int? TransferWorkflowId { get; set; }

  public int SourceAddressId { get; set; }

    public int DestinationAddressId { get; set; }

     public DateTime Day { get; set; }

    public decimal SubmittedKwh { get; set; } //what we submitted to provider/grid for settlement
    
    public decimal SettledKwh { get; set; } //what provider accepted/credited  

    public decimal RatePerKwh { get; set; }

    public decimal MonetaryCredit { get; set; }

    public decimal EnergyCreditKwh { get; set; }

    public int SettlementMode { get; set; }

     public string? Note { get; set; }

    public virtual TransferWorkflow? TransferWorkflow { get; set; }

}
