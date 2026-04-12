using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Infrastructure.DTOs
{
    public class 
    TransferRuleDto
    {
        [BindNever]
        public int Id { get; set; }
        public int SourceAddressId { get; set; }
        public int DestinationAddressId { get; set; }
        public bool IsEnabled { get; set; }
        public int Priority { get; set; }
        public int DistributionMode { get; set; }
        public decimal? MaxDailyKwh { get; set; }
        public decimal? WeightPercent { get; set; }
    }
}
