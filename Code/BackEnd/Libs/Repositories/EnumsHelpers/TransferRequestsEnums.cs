using Infrastructure.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repositories.Models
{
    public partial class TransferRequest
    {
        
        [NotMapped]
        public ProviderSettlementMode SettlementModeEnum
        {
            get => (ProviderSettlementMode)this.SettlementMode;
            set => this.SettlementMode = (int)value;
        }


        [NotMapped]
        public TransferStatus StatusEnum
        {
            get => (TransferStatus)this.Status;
            set => this.Status = (int)value;
        }
    }
}