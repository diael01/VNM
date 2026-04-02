using Repositories.Models;
namespace EnergyManagement.Services.ModeSwitching;
public class MoneySettlementModeStrategy : ISettlementModeStrategy
    {
        public SettlementMode SettlementMode => SettlementMode.Money;

        public void FillSettlement(
            ProviderSettlement settlement,
            DailyEnergyBalance balance,
            decimal ratePerKwh,
            decimal acceptanceRate)
        {
            var injected = balance.SurplusKwh;
            var accepted = injected * acceptanceRate;

            settlement.InjectedKwh = injected;
            settlement.AcceptedKwh = accepted;
            settlement.RatePerKwh = ratePerKwh;
            settlement.MonetaryCredit = accepted * ratePerKwh;
            settlement.EnergyCreditKwh = 0;
            settlement.SettlementMode = SettlementMode;
        }

        public void ValidateRequest(decimal amount, AvailableTransferBalanceDto available)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Amount must be > 0");

            if (amount > available.AvailableMoney)
                throw new InvalidOperationException("Not enough money balance");
        }

        public void FillTransferAmounts(TransferRequest transfer, decimal amount)
        {
            transfer.RequestedAmount = amount;
            transfer.ActualAmount = amount;
            transfer.RequestedAmount = 0;
            transfer.ActualAmount = 0;
            transfer.SettlementMode = SettlementMode;
        }

        public TransferImpactDto BuildImpact(
            TransferRequest transfer,
            DailyEnergyBalance balance,
            decimal rate)
        {
            var originalCost = (balance.DeficitKwh) * rate;
            var coveredKwh = transfer.ActualAmount / rate;

            return new TransferImpactDto
            {
                DestinationAddressId = transfer.DestinationAddressId,
                Day = transfer.Day,
                OriginalDeficitKwh = balance.DeficitKwh,
                CoveredByTransferKwh = coveredKwh,
                RemainingDeficitKwh = Math.Max((balance.DeficitKwh) - coveredKwh, 0m),
                OriginalCost = originalCost,
                CoveredValue = transfer.ActualAmount,
                RemainingCost = Math.Max(originalCost - transfer.ActualAmount, 0m)
            };
        }
    }