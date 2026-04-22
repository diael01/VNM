namespace Infrastructure.Enums;
public enum ProviderSettlementMode
{
    Money = 0,
    EnergyCredit = 1
}

public enum TransferDistributionMode
{
    Fair = 0,
    Priority = 1,
    Weighted = 2
}

/* What each status means
Planned = proposal exists
Approved = user accepted it
Executed = system actually performed it
Settled = accounting/legal/provider settlement completed
Rejected = user explicitly refused the plan
Cancelled = previously acceptable row was stopped/voided before execution
Failed = execution was attempted but failed */
public enum TransferStatus
{
    Planned = 0,
    Approved = 1,
    Executed = 2,
    Settled = 3,
    Rejected = 4,
    Cancelled = 5,
    Failed = 6  
}



public enum TriggerType
{
    Auto = 0,
    Manual = 1,
}

public enum ScheduleType
{
    Once = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Interval = 4
}

public enum RepeatEveryUnit
{
    Minutes = 0,
    Hours = 1
}

public enum ExecutionMode
{
    PlanOnly = 0,            // Only create TransferWorkflow rows (Status = Planned)

    PlanAndApprove = 1,      // Create + mark as Approved (skips manual approval step)

    PlanAndExecute = 2       // Create + execute immediately (updates balances)
}

public enum InverterStatus
{
    Offline,
    Standby,
    Producing,
    Fault
}
