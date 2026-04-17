using System;

namespace Repositories.Models;

public partial class SourceTransferSchedule : AuditableEntity
{
    public int Id { get; set; }

    public int SourceTransferPolicyId { get; set; }

    public bool IsEnabled { get; set; }

    public int ScheduleType { get; set; } // Once, Daily, Weekly, Monthly, Custom
    public int ExecutionMode { get; set; } // PlanOnly, PlanAndApprove, PlanAndExecute, etc.

    public DateTime StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }

    public TimeSpan? TimeOfDayUtc { get; set; }

    public int? IntervalMinutes { get; set; }   // optional for repeated intra-day runs
    public int? DayOfWeek { get; set; }         // for weekly
    public int? DayOfMonth { get; set; }        // for monthly

    public DateTime? LastRunUtc { get; set; }
    public DateTime? NextRunUtc { get; set; }

    public virtual SourceTransferPolicy SourceTransferPolicy { get; set; } = null!;
}