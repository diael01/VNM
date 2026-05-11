using Infrastructure.Enums;
using Repositories.Models;

namespace EnergyManagement.Services.ModeSwitching;

public class MoneySettlementModeStrategy : ISettlementModeStrategy
{
    public ProviderSettlementMode SettlementMode => ProviderSettlementMode.Monetary;

    public void FillSettlement(
        ProviderSettlement settlement,
        DailyEnergyBalance balance,
        decimal ratePerKwh,
        decimal acceptanceRate)
    {
        FillSettlementFromExecutedWorkflowAmount(
            settlement,
            balance.SurplusKwh,
            ratePerKwh,
            acceptanceRate);
    }

    public void FillSettlementFromExecutedWorkflowAmount(
        ProviderSettlement settlement,
        decimal executedKwh,
        decimal ratePerKwh,
        decimal acceptanceRate)
    {
        var injected = executedKwh;
        var accepted = injected * acceptanceRate;

        settlement.SubmittedKwh = injected;
        settlement.SettledKwh = accepted;
        settlement.RatePerKwh = ratePerKwh;
        settlement.MonetaryCredit = accepted * ratePerKwh;
        settlement.EnergyCreditKwh = 0;
        settlement.CreatedAtUtc = DateTime.UtcNow;
        settlement.SettlementModeEnum = SettlementMode;
    }

    public void ValidateRequest(decimal amount, AvailableTransferBalanceDto available)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount must be > 0");

        if (amount > available.AvailableMoney)
            throw new InvalidOperationException("Not enough money balance");
    }

    public void FillTransferAmounts(TransferExecutionRequest transfer, decimal amount)
    {
       /*  transfer.RequestedAmount = amount;
        transfer.ActualAmount = amount;
        transfer.RequestedAmount = 0;
        transfer.ActualAmount = 0;
        transfer.SettlementModeEnum = SettlementMode; */
    }

    public TransferImpactDto BuildImpact(
        TransferExecutionRequest transfer,
        DailyEnergyBalance balance,
        decimal rate)
    {
        var originalCost = balance.DeficitKwh * rate;
        var coveredKwh = transfer.AmountKwh / rate;

        return new TransferImpactDto
        {
            DestinationAddressId = transfer.DestinationAddressId,
            Day = transfer.BalanceDay,
            OriginalDeficitKwh = balance.DeficitKwh,
            CoveredByTransferKwh = coveredKwh,
            RemainingDeficitKwh = Math.Max(balance.DeficitKwh - coveredKwh, 0m),
            OriginalCost = originalCost,
            CoveredValue = transfer.AmountKwh,
            RemainingCost = Math.Max(originalCost - transfer.AmountKwh, 0m)
        };
    }
}
