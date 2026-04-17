using System;
using System.ComponentModel.DataAnnotations.Schema;
using Infrastructure.Enums;

namespace Repositories.Models;

public partial class SourceTransferSchedule 
{
    [NotMapped]
    public ScheduleType ScheduleTypeEnum
    {
        get => (ScheduleType)ScheduleType;
        set => ScheduleType = (int)value;
    }

    [NotMapped]
    public ExecutionMode ExecutionModeEnum
    {
        get => (ExecutionMode)ExecutionMode;
        set => ExecutionMode = (int)value;
    }

    [NotMapped]
    public RepeatEveryUnit? RepeatEveryUnitEnum
    {
        get => RepeatEveryUnit.HasValue ? (RepeatEveryUnit)RepeatEveryUnit.Value : null;
        set => RepeatEveryUnit = value.HasValue ? (int)value.Value : null;
    }
}