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
        public int? RepeatEveryValue { get; set; } //the number of units between runs, e.g. every 2 minutes
        public int? RepeatEveryUnit { get; set; } //the unit of time for the interval, e.g. minutes, hours
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
