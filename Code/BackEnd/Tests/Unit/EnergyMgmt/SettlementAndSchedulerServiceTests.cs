using EnergyManagement.Services.ModeSwitching;
using EnergyManagement.Services.Providers;
using EnergyManagement.Services.Transfers;
using Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repositories.Models;
using Xunit;

namespace Tests.Transfers;

public class SettlementAndSchedulerServiceTests
{
    private static VnmDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<VnmDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new VnmDbContext(options);
    }

    [Fact]
    public async Task AvailableBalance_ReturnsEmpty_WhenNoSettlementExists()
    {
        await using var db = CreateContext("AvailableBalance_Empty");
        var sut = new AvailableBalanceService(db);

        var balance = await sut.GetAvailableBalanceAsync(10, new DateOnly(2026, 5, 11));

        Assert.Equal(0, balance.AddressId);
        Assert.Equal(0m, balance.AvailableMoney);
        Assert.Equal(0m, balance.AvailableKwh);
    }

    [Fact]
    public async Task AvailableBalance_ReturnsStoredAmounts_WhenSettlementExists()
    {
        await using var db = CreateContext("AvailableBalance_Present");
        db.ProviderSettlements.Add(new ProviderSettlement
        {
            SourceAddressId = 10,
            DestinationAddressId = 20,
            Day = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc),
            MonetaryCredit = 12.5m,
            EnergyCreditKwh = 7.25m,
            SettlementMode = (int)ProviderSettlementMode.Monetary
        });
        await db.SaveChangesAsync();

        var sut = new AvailableBalanceService(db);

        var balance = await sut.GetAvailableBalanceAsync(10, new DateOnly(2026, 5, 11));

        Assert.Equal(10, balance.AddressId);
        Assert.Equal(new DateOnly(2026, 5, 11), balance.Day);
        Assert.Equal(12.5m, balance.AvailableMoney);
        Assert.Equal(7.25m, balance.AvailableKwh);
    }

    [Fact]
    public async Task ProviderSettlement_ProcessSettlement_CreatesEnergyCreditSettlement()
    {
        await using var db = CreateContext("ProviderSettlement_Create");
        db.DailyEnergyBalances.Add(new DailyEnergyBalance
        {
            AddressId = 10,
            Day = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc),
            ProducedKwh = 10,
            ConsumedKwh = 0,
            SurplusKwh = 10,
            DeficitKwh = 0,
            CalculatedAtUtc = DateTime.UtcNow,
            Status = "Computed",
            NetKwh = 10,
            NetPerAddressKwh = 10
        });
        await db.SaveChangesAsync();

        var resolver = new Mock<ISettlementModeResolver>();
        var energyStrategy = new EnergySettlementModeStrategy();
        resolver.Setup(x => x.GetCurrentMode()).Returns(ProviderSettlementMode.EnergyCredit);
        resolver.Setup(x => x.Resolve(ProviderSettlementMode.EnergyCredit)).Returns(energyStrategy);

        var sut = new ProviderSettlementService(db, resolver.Object);

        var settlement = await sut.ProcessSettlementAsync(10, 20, new DateOnly(2026, 5, 11));

        Assert.Equal(10, settlement.SourceAddressId);
        Assert.Equal(20, settlement.DestinationAddressId);
        Assert.Null(settlement.TransferWorkflowId);
        Assert.Equal(10m, settlement.SubmittedKwh);
        Assert.Equal(10m, settlement.SettledKwh);
        Assert.Equal(0m, settlement.MonetaryCredit);
        Assert.Equal(10m, settlement.EnergyCreditKwh);
        Assert.Equal(ProviderSettlementMode.EnergyCredit, settlement.SettlementModeEnum);
    }

    [Fact]
    public async Task ProviderSettlement_ProcessSettlement_ReturnsExisting_WhenAlreadyPresent()
    {
        await using var db = CreateContext("ProviderSettlement_Existing");
        var existing = new ProviderSettlement
        {
            SourceAddressId = 10,
            DestinationAddressId = 20,
            Day = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc),
            MonetaryCredit = 5m,
            EnergyCreditKwh = 2m,
            SettlementMode = (int)ProviderSettlementMode.Monetary
        };
        db.ProviderSettlements.Add(existing);
        await db.SaveChangesAsync();

        var resolver = new Mock<ISettlementModeResolver>();
        var sut = new ProviderSettlementService(db, resolver.Object);

        var settlement = await sut.ProcessSettlementAsync(10, 20, new DateOnly(2026, 5, 11));

        Assert.Equal(existing.Id, settlement.Id);
        resolver.Verify(x => x.Resolve(It.IsAny<ProviderSettlementMode>()), Times.Never);
    }

    [Fact]
    public async Task ProviderSettlement_SettleWorkflow_CreatesSnapshotAndHistory()
    {
        await using var db = CreateContext("ProviderSettlement_Workflow");
        db.TransferWorkflows.Add(new TransferWorkflow
        {
            Id = 1,
            SourceAddressId = 10,
            DestinationAddressId = 20,
            BalanceDayUtc = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc),
            EffectiveAtUtc = DateTime.UtcNow,
            SourceSurplusKwhAtWorkflow = 10,
            DestinationDeficitKwhAtWorkflow = 5,
            AmountKwh = 4,
            Status = (int)TransferStatus.Executed,
            TriggerType = (int)TriggerType.Manual,
            SettlementMode = 0,
            AppliedDistributionMode = 0
        });
        await db.SaveChangesAsync();

        var resolver = new Mock<ISettlementModeResolver>();
        resolver.Setup(x => x.GetCurrentMode()).Returns(ProviderSettlementMode.Monetary);
        resolver.Setup(x => x.Resolve(ProviderSettlementMode.Monetary)).Returns(new MoneySettlementModeStrategy());

        var sut = new ProviderSettlementService(db, resolver.Object);

        var settlement = await sut.SettleWorkflowAsync(1, "done");

        Assert.Equal(1, settlement.TransferWorkflowId);
        Assert.Equal(10, settlement.SourceAddressId);
        Assert.Equal(20, settlement.DestinationAddressId);
        Assert.Equal(new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc), settlement.Day);
        Assert.Equal(4m, settlement.SubmittedKwh);
        Assert.Equal(4m, settlement.SettledKwh);
        Assert.Equal(3.2m, settlement.MonetaryCredit);

        var workflow = await db.TransferWorkflows.SingleAsync(x => x.Id == 1);
        Assert.Equal((int)TransferStatus.Settled, workflow.Status);

        var history = await db.TransferWorkflowStatusHistory.SingleAsync(x => x.TransferWorkflowId == 1);
        Assert.Equal((int)TransferStatus.Executed, history.FromStatus);
        Assert.Equal((int)TransferStatus.Settled, history.ToStatus);
        Assert.Equal("done", history.Note);
    }

    [Fact]
    public async Task ProviderSettlement_SettleWorkflow_ThrowsWhenWorkflowNotExecuted()
    {
        await using var db = CreateContext("ProviderSettlement_NotExecuted");
        db.TransferWorkflows.Add(new TransferWorkflow
        {
            Id = 1,
            SourceAddressId = 10,
            DestinationAddressId = 20,
            BalanceDayUtc = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc),
            EffectiveAtUtc = DateTime.UtcNow,
            SourceSurplusKwhAtWorkflow = 10,
            DestinationDeficitKwhAtWorkflow = 5,
            AmountKwh = 4,
            Status = (int)TransferStatus.Approved,
            TriggerType = (int)TriggerType.Manual,
            SettlementMode = 0,
            AppliedDistributionMode = 0
        });
        await db.SaveChangesAsync();

        var resolver = new Mock<ISettlementModeResolver>();
        var sut = new ProviderSettlementService(db, resolver.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SettleWorkflowAsync(1));

        Assert.Contains("Only executed workflows can be settled", ex.Message);
    }

    [Fact]
    public async Task Scheduler_RunAutomaticWorkflow_CreatesPlannedWorkflow()
    {
        await using var db = CreateContext("Scheduler_Create");

        var policy = new SourceTransferPolicy
        {
            Id = 1,
            SourceAddressId = 10,
            DistributionMode = (int)TransferDistributionMode.Fair,
            IsEnabled = true
        };
        db.SourceTransferPolicies.Add(policy);
        db.DestinationTransferRules.Add(new DestinationTransferRule
        {
            Id = 1,
            SourceTransferPolicy = policy,
            SourceTransferPolicyId = 1,
            DestinationAddressId = 20,
            DistributionMode = (int)TransferDistributionMode.Fair,
            IsEnabled = true,
            Priority = 1,
            MaxDailyKwh = null,
            WeightPercent = null
        });
        db.DailyEnergyBalances.AddRange(
            new DailyEnergyBalance
            {
                AddressId = 10,
                Day = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc),
                ProducedKwh = 10,
                ConsumedKwh = 0,
                SurplusKwh = 10,
                DeficitKwh = 0,
                CalculatedAtUtc = DateTime.UtcNow,
                Status = "Computed",
                NetKwh = 10,
                NetPerAddressKwh = 10
            },
            new DailyEnergyBalance
            {
                AddressId = 20,
                Day = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc),
                ProducedKwh = 0,
                ConsumedKwh = 4,
                SurplusKwh = 0,
                DeficitKwh = 4,
                CalculatedAtUtc = DateTime.UtcNow,
                Status = "Computed",
                NetKwh = -4,
                NetPerAddressKwh = -4
            });
        await db.SaveChangesAsync();

        var sut = new TransferWorkflowScheduledService(db, NullLogger<TransferWorkflowScheduledService>.Instance);

        var workflows = await sut.RunAutomaticWorkflowForSourceAsync(10, new DateOnly(2026, 5, 11));

        Assert.Single(workflows);
        var workflow = workflows[0];
        Assert.Equal(10, workflow.SourceAddressId);
        Assert.Equal(20, workflow.DestinationAddressId);
        Assert.Equal((int)TransferStatus.Planned, workflow.Status);
        Assert.Equal((int)TriggerType.Auto, workflow.TriggerType);
        Assert.Equal((int)TransferDistributionMode.Fair, workflow.AppliedDistributionMode);
        Assert.Equal(4m, workflow.AmountKwh);
    }

    [Fact]
    public async Task Scheduler_RunAutomaticWorkflow_DeletesExistingPlannedRows_WhenNoSurplus()
    {
        await using var db = CreateContext("Scheduler_Delete");

        db.TransferWorkflows.Add(new TransferWorkflow
        {
            Id = 1,
            SourceAddressId = 10,
            DestinationAddressId = 20,
            BalanceDayUtc = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc),
            SourceSurplusKwhAtWorkflow = 5,
            DestinationDeficitKwhAtWorkflow = 2,
            AmountKwh = 2,
            Status = (int)TransferStatus.Planned,
            TriggerType = (int)TriggerType.Auto,
            SettlementMode = 0,
            AppliedDistributionMode = (int)TransferDistributionMode.Fair
        });
        db.SourceTransferPolicies.Add(new SourceTransferPolicy
        {
            Id = 1,
            SourceAddressId = 10,
            DistributionMode = (int)TransferDistributionMode.Fair,
            IsEnabled = true
        });
        db.DailyEnergyBalances.Add(new DailyEnergyBalance
        {
            AddressId = 10,
            Day = new DateTime(2026, 5, 11, 0, 0, 0, DateTimeKind.Utc),
            ProducedKwh = 0,
            ConsumedKwh = 0,
            SurplusKwh = 0,
            DeficitKwh = 0,
            CalculatedAtUtc = DateTime.UtcNow,
            Status = "Computed",
            NetKwh = 0,
            NetPerAddressKwh = 0
        });
        await db.SaveChangesAsync();

        var sut = new TransferWorkflowScheduledService(db, NullLogger<TransferWorkflowScheduledService>.Instance);

        var workflows = await sut.RunAutomaticWorkflowForSourceAsync(10, new DateOnly(2026, 5, 11));

        Assert.Empty(workflows);
        Assert.Empty(await db.TransferWorkflows.ToListAsync());
    }
}
