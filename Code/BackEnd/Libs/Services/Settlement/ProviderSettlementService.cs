/* =========================================================
   SERVICES - PROVIDER
   ========================================================= */

using EnergyManagement.Services.ModeSwitching;
using Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;
using Repositories.Models;

namespace EnergyManagement.Services.Providers
{
    public class ProviderSettlementService : IProviderSettlementService
    {
        private readonly VnmDbContext _db;
        private readonly ISettlementModeResolver _resolver;

        // TODO: move these to options when you are ready to model real provider contracts.
        private const decimal DefaultRatePerKwh = 0.8m;
        private const decimal DefaultAcceptanceRate = 1.0m;

        public ProviderSettlementService(VnmDbContext db, ISettlementModeResolver resolver)
        {
            _db = db;
            _resolver = resolver;
        }

        /// <summary>
        /// Daily/provider reconciliation settlement for a source -> destination pair.
        /// This is NOT tied to one transfer workflow.
        ///
        /// Important:
        /// - sourceAddressId and destinationAddressId must NOT be the same.
        /// - Day stays here because daily settlements are unique per source/destination/day.
        /// - TransferWorkflowId stays null because this method is not settling one workflow row.
        ///
        /// Uniqueness rule:
        /// SourceAddressId + DestinationAddressId + Day + TransferWorkflowId == null.
        /// </summary>
        public async Task<ProviderSettlement> ProcessSettlementAsync(
            int sourceAddressId,
            int destinationAddressId,
            DateOnly day,
            CancellationToken ct = default)
        {
            if (sourceAddressId == destinationAddressId)
                throw new InvalidOperationException("Source and destination address cannot be the same.");

            var settlementDay = day.ToDateTime(TimeOnly.MinValue);

            var existingSettlement = await _db.ProviderSettlements
                .FirstOrDefaultAsync(x =>
                    x.SourceAddressId == sourceAddressId &&
                    x.DestinationAddressId == destinationAddressId &&
                    x.Day == settlementDay &&
                    x.TransferWorkflowId == null,
                    ct);

            if (existingSettlement is not null)
                return existingSettlement;

            // For pair-based daily settlement, the submitted energy comes from the source balance.
            // The destination is stored on the settlement snapshot, but we do not use the destination
            // balance here because this method settles what the source submits to the provider/grid.
            var sourceBalance = await _db.DailyEnergyBalances
                .FirstAsync(x =>
                    x.AddressId == sourceAddressId &&
                    DateOnly.FromDateTime(x.Day) == day,
                    ct);

            var mode = _resolver.GetCurrentMode();
            var strategy = _resolver.Resolve(mode);

            var settlement = new ProviderSettlement
            {
                SourceAddressId = sourceAddressId,
                DestinationAddressId = destinationAddressId,
                TransferWorkflowId = null,
                Day = settlementDay
            };

            strategy.FillSettlement(
                settlement,
                sourceBalance,
                DefaultRatePerKwh,
                DefaultAcceptanceRate);

            _db.ProviderSettlements.Add(settlement);
            await _db.SaveChangesAsync(ct);

            return settlement;
        }

        /// <summary>
        /// UI Settle button.
        /// Only an executed workflow can be settled.
        /// This creates a provider settlement snapshot from the executed workflow amount,
        /// not from current daily balance numbers, because balances may have changed.
        ///
        /// Uniqueness rule: one ProviderSettlement per TransferWorkflowId.
        /// </summary>
        public async Task<ProviderSettlement> SettleWorkflowAsync(
            int workflowId,
            string? note = null,
            CancellationToken ct = default)
        {
            var workflow = await _db.TransferWorkflows
                .FirstOrDefaultAsync(x => x.Id == workflowId, ct);

            if (workflow is null)
                throw new InvalidOperationException($"Transfer workflow {workflowId} was not found.");

            if (workflow.SourceAddressId == workflow.DestinationAddressId)
                throw new InvalidOperationException("Source and destination address cannot be the same.");

            if (workflow.TransferStatusEnum != TransferStatus.Executed)
                throw new InvalidOperationException("Only executed workflows can be settled.");

            var existingSettlement = await _db.ProviderSettlements
                .FirstOrDefaultAsync(x => x.TransferWorkflowId == workflow.Id, ct);

            if (existingSettlement is not null)
                return existingSettlement;

            var mode = _resolver.GetCurrentMode();
            var strategy = _resolver.Resolve(mode);
            var now = DateTime.UtcNow;

            var settlement = new ProviderSettlement
            {
                SourceAddressId = workflow.SourceAddressId,
                DestinationAddressId = workflow.DestinationAddressId,
                TransferWorkflowId = workflow.Id,
                Day = workflow.BalanceDayUtc.Date,
                Note = note
            };

            strategy.FillSettlementFromExecutedWorkflowAmount(
                settlement,
                workflow.AmountKwh,
                DefaultRatePerKwh,
                DefaultAcceptanceRate);

            _db.ProviderSettlements.Add(settlement);

            _db.Set<TransferWorkflowStatusHistory>().Add(new TransferWorkflowStatusHistory
            {
                TransferWorkflowId = workflow.Id,
                FromStatus = workflow.Status,
                ToStatus = (int)TransferStatus.Settled,
                UpdatedAtUtc = now,
                Note = note
            });

            workflow.TransferStatusEnum = TransferStatus.Settled;
            workflow.EffectiveAtUtc = now;

            await _db.SaveChangesAsync(ct);

            return settlement;
        }
    }
}
