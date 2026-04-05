using Infrastructure.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repositories.Models
{
    public partial class TransferExecution
    {
        [NotMapped]
        public TriggerType TriggerTypeEnum
        {
            get => (TriggerType)TriggerType;
            set => TriggerType = (int)value;
        }

        [NotMapped]
        public TransferStatus TransferStatusEnum
        {
            get => (TransferStatus)this.Status;
            set => this.Status = (int)value;
        }
    }
}