using Infrastructure.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Repositories.Models;

/* =========================================================
   MODE SWITCHING (CORE)
   ========================================================= */

namespace EnergyManagement.Services.ModeSwitching
{
  
    public interface ISettlementModeStrategy
    {
        ProviderSettlementMode SettlementMode { get; }

        void FillSettlement(
            ProviderSettlement settlement,
            DailyEnergyBalance balance,
            decimal ratePerKwh,
            decimal acceptanceRate);

        void ValidateRequest(
            decimal requestedAmount,
            AvailableTransferBalanceDto available);

        void FillTransferAmounts(
            TransferRequest transfer,
            decimal requestedAmount);

        TransferImpactDto BuildImpact(
            TransferRequest transfer,
            DailyEnergyBalance destinationBalance,
            decimal ratePerKwh);
    }
}
