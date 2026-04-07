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

public enum TransferStatus
{
    Planned = 0,
    Approved = 1,
    Executed = 2,
    Settled = 3,
    Rejected = 4
}

public enum TriggerType
{
    Auto = 0,
    Manual = 1,
}