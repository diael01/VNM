using Infrastructure.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repositories.Models
{
    public partial class TransferRule
    {
        [NotMapped]
        public TransferDistributionMode DistributionModeEnum
        {
            get => (TransferDistributionMode)DistributionMode;
            set => DistributionMode = (int)value;
        }
    }
}