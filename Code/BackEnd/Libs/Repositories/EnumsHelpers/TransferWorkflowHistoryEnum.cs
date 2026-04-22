using System.ComponentModel.DataAnnotations.Schema;
using Infrastructure.Enums;

namespace Repositories.Models;

public partial class TransferWorkflowStatusHistory
{
[NotMapped]
public TransferStatus? FromStatusEnum
{
    get => FromStatus.HasValue ? (TransferStatus)FromStatus.Value : null;
    set => FromStatus = value.HasValue ? (int)value.Value : null;
}

[NotMapped]
public TransferStatus ToStatusEnum
{
    get => (TransferStatus)ToStatus;
    set => ToStatus = (int)value;
}
}