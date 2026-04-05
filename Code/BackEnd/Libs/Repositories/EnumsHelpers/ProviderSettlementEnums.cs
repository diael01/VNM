using Infrastructure.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repositories.Models
{
    public partial class ProviderSettlement
    {
        [NotMapped]
        public ProviderSettlementMode SettlementModeEnum
        {
            get => (ProviderSettlementMode)this.SettlementMode;
            set => this.SettlementMode = (int)value;
        }
    }
}
