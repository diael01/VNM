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

        /// <summary>
        /// Used when settling a specific executed workflow.
        /// Do not recalculate from the current DailyEnergyBalance here; the workflow was already executed,
        /// so settlement must use the executed workflow amount snapshot.
        /// </summary>
        void FillSettlementFromExecutedWorkflowAmount(
            ProviderSettlement settlement,
            decimal executedKwh,
            decimal ratePerKwh,
            decimal acceptanceRate);

        void ValidateRequest(
            decimal requestedAmount,
            AvailableTransferBalanceDto available);

        void FillTransferAmounts(
            TransferExecutionRequest transfer,
            decimal requestedAmount);

        TransferImpactDto BuildImpact(
            TransferExecutionRequest transfer,
            DailyEnergyBalance destinationBalance,
            decimal ratePerKwh);
    }
}
