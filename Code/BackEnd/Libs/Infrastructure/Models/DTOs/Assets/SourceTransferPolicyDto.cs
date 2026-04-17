using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Infrastructure.DTOs
{
    public class SourceTransferPolicyDto
    {
        [BindNever]
        public int Id { get; set; }
        public int SourceAddressId { get; set; }
        public int DistributionMode { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        // Computed summary fields (read-only from API, not used on write)
        public int DestinationRulesCount { get; set; }
        public int SchedulesCount { get; set; }
    }
}
