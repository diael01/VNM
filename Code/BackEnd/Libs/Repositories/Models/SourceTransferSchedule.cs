using System;
using System.ComponentModel.DataAnnotations.Schema;
using Infrastructure.Enums;

namespace Repositories.Models;

public partial class SourceTransferSchedule : AuditableEntity
{
    public int Id { get; set; }

    public int SourceTransferPolicyId { get; set; }

    public bool IsEnabled { get; set; } //the automation switch

    // Once, Daily, Weekly, Monthly, Interval
    public int ScheduleType { get; set; }

    // PlanOnly, PlanAndApprove, PlanAndExecute, etc.
    public int ExecutionMode { get; set; }

    public DateTime StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }

    // Used by Daily / Weekly / Monthly schedules
    public TimeSpan? TimeOfDayUtc { get; set; }

    // Used by Interval schedule
    public int? RepeatEveryValue { get; set; } //the number of units between runs, e.g. every 2 minutes

    // Minutes, Hours
    public int? RepeatEveryUnit { get; set; } //the unit of time for the interval, e.g. minutes, hours

    // Used by Weekly schedule
    public int? DayOfWeek { get; set; }

    // Used by Monthly schedule
    public int? DayOfMonth { get; set; }

    public DateTime? LastRunUtc { get; set; }
    public DateTime? NextRunUtc { get; set; }

    public virtual SourceTransferPolicy SourceTransferPolicy { get; set; } = null!;

   
}

//IsEnabled meaning:
/* 3. SourceTransferSchedule.IsEnabled
This is the automation switch.

Meaning:
Is this schedule active?
Example:
policy exists and is enabled
destination rules exist
but automation is disabled because user wants manual-only operation

Or:
two schedules exist
daily one enabled
weekly one disabled 


What “automation” should mean
This is the most important point:
Automation should not start just because the user saved a policy.
Saving a policy means only:
the configuration exists
It should not automatically mean:
background jobs will start using it
That would be dangerous and confusing.


So in practice:
policy saved → not necessarily automated
destinations saved → not necessarily automated
schedule saved but disabled → not automated
schedule saved and enabled → automated
That is the right interpretation.

Automation Active = true only if there is at least one enabled schedule

So is a policy “automated” once saved?
No.
It is automated only if:
it has at least one schedule
that schedule is enabled
and the policy itself is enabled
*/