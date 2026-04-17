using Infrastructure.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repositories.Models
{
    public partial class DestinationTransferRule
    {
        [NotMapped]
        public TransferDistributionMode DistributionModeEnum
        {
            get => (TransferDistributionMode)DistributionMode;
            set => DistributionMode = (int)value;
        }
    }
}