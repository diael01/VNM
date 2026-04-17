using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Infrastructure.DTOs
{
    public class SourceTransferScheduleDto
    {
        [BindNever]
        public int Id { get; set; }
        public int SourceTransferPolicyId { get; set; }
        public bool IsEnabled { get; set; }
        public int ScheduleType { get; set; }
        public int ExecutionMode { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime? EndDateUtc { get; set; }
        public TimeSpan? TimeOfDayUtc { get; set; }
        public int? IntervalMinutes { get; set; }
        public int? DayOfWeek { get; set; }
        public int? DayOfMonth { get; set; }
        public DateTime? LastRunUtc { get; set; }
        public DateTime? NextRunUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
