using System;

namespace Repositories.Models;

public partial class TransferWorkflowStatusHistory : AuditableEntity
{
    public int Id { get; set; }

    public int TransferWorkflowId { get; set; }

    public int? FromStatus { get; set; }

    public int ToStatus { get; set; }

    public string? Note { get; set; }

    public virtual TransferWorkflow TransferWorkflow { get; set; } = null!;
}
