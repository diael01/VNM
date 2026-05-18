using EnergyManagement.Services.Transfers.Execution;
using Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repositories.Models;
using Xunit;

namespace Tests.Transfers;

public class TransferExecutionServiceTests
{
    private static VnmDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<VnmDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new VnmDbContext(options);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWorkflowMissing_Throws()
    {
        await using var db = CreateContext("TransferExecution_MissingWorkflow");
        var adapter = new Mock<ITransferExecutionAdapter>();
        var sut = new TransferExecutionService(db, adapter.Object, NullLogger<TransferExecutionService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(99, "tester", null, CancellationToken.None));

        Assert.Contains("Transfer workflow 99 was not found", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWorkflowNotApproved_Throws()
    {
        await using var db = CreateContext("TransferExecution_NotApproved");
        db.TransferWorkflows.Add(new TransferWorkflow
        {
            Id = 1,
            EffectiveAtUtc = DateTime.UtcNow,
            BalanceDayUtc = DateTime.UtcNow.Date,
            SourceAddressId = 10,
            DestinationAddressId = 20,
            SourceSurplusKwhAtWorkflow = 10,
            DestinationDeficitKwhAtWorkflow = 8,
            AmountKwh = 5,
            TriggerType = (int)TriggerType.Manual,
            Status = (int)TransferStatus.Planned,
            SettlementMode = 0,
            AppliedDistributionMode = 0
        });
        await db.SaveChangesAsync();

        var adapter = new Mock<ITransferExecutionAdapter>();
        var sut = new TransferExecutionService(db, adapter.Object, NullLogger<TransferExecutionService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(1, "tester", null, CancellationToken.None));

        Assert.Contains("must be Approved before execution", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoExecutableAmount_Throws()
    {
        await using var db = CreateContext("TransferExecution_NoExecutableAmount");

        db.TransferWorkflows.Add(new TransferWorkflow
        {
            Id = 1,
            EffectiveAtUtc = DateTime.UtcNow,
            BalanceDayUtc = DateTime.UtcNow.Date,
            SourceAddressId = 10,
            DestinationAddressId = 20,
            SourceSurplusKwhAtWorkflow = 10,
            DestinationDeficitKwhAtWorkflow = 8,
            AmountKwh = 5,
            TriggerType = (int)TriggerType.Manual,
            Status = (int)TransferStatus.Approved,
            SettlementMode = 0,
            AppliedDistributionMode = 0
        });

        db.DailyEnergyBalances.AddRange(
            new DailyEnergyBalance
            {
                AddressId = 10,
                Day = DateTime.UtcNow.Date,
                ProducedKwh = 0,
                ConsumedKwh = 0,
                SurplusKwh = 0,
                DeficitKwh = 0,
                CalculatedAtUtc = DateTime.UtcNow,
                Status = "Computed",
                NetKwh = 0,
                NetPerAddressKwh = 0
            },
            new DailyEnergyBalance
            {
                AddressId = 20,
                Day = DateTime.UtcNow.Date,
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

        var adapter = new Mock<ITransferExecutionAdapter>();
        var sut = new TransferExecutionService(db, adapter.Object, NullLogger<TransferExecutionService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(1, "tester", null, CancellationToken.None));

        Assert.Contains("current transferable amount is 0 kWh", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdapterFails_MarksWorkflowDiscontinuedAndAddsHistory()
    {
        await using var db = CreateContext("TransferExecution_AdapterFails");

        db.TransferWorkflows.Add(new TransferWorkflow
        {
            Id = 1,
            EffectiveAtUtc = DateTime.UtcNow,
            BalanceDayUtc = DateTime.UtcNow.Date,
            SourceAddressId = 10,
            DestinationAddressId = 20,
            SourceSurplusKwhAtWorkflow = 10,
            DestinationDeficitKwhAtWorkflow = 8,
            AmountKwh = 5,
            TriggerType = (int)TriggerType.Manual,
            Status = (int)TransferStatus.Approved,
            SettlementMode = 0,
            AppliedDistributionMode = 0
        });

        db.DailyEnergyBalances.AddRange(
            new DailyEnergyBalance
            {
                AddressId = 10,
                Day = DateTime.UtcNow.Date,
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
                Day = DateTime.UtcNow.Date,
                ProducedKwh = 0,
                ConsumedKwh = 8,
                SurplusKwh = 0,
                DeficitKwh = 8,
                CalculatedAtUtc = DateTime.UtcNow,
                Status = "Computed",
                NetKwh = -8,
                NetPerAddressKwh = -8
            });

        await db.SaveChangesAsync();

        var adapter = new Mock<ITransferExecutionAdapter>();
        adapter.Setup(a => a.ExecuteAsync(It.IsAny<TransferExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransferExecutionResult.Failed("boom"));

        var sut = new TransferExecutionService(db, adapter.Object, NullLogger<TransferExecutionService>.Instance);

        await sut.ExecuteAsync(1, "tester", "  failed attempt  ", CancellationToken.None);

        var workflow = await db.TransferWorkflows.SingleAsync(x => x.Id == 1);
        Assert.Equal((int)TransferStatus.Discontinued, workflow.Status);
        Assert.Equal("system", workflow.UpdatedBy);

        var history = await db.TransferWorkflowStatusHistory.SingleAsync(x => x.TransferWorkflowId == 1);
        Assert.Equal((int)TransferStatus.Approved, history.FromStatus);
        Assert.Equal((int)TransferStatus.Discontinued, history.ToStatus);
        Assert.Equal("tester", history.UpdatedBy);
        Assert.Contains("Failed while performing ExecuteWorkflowAsync: boom", history.Note);
        Assert.Contains("UserNote=failed attempt", history.Note);

        Assert.Empty(await db.TransferLedgerEntries.ToListAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdapterSucceeds_CreatesLedgerAndMarksExecuted()
    {
        await using var db = CreateContext("TransferExecution_AdapterSuccess");

        db.TransferWorkflows.Add(new TransferWorkflow
        {
            Id = 1,
            EffectiveAtUtc = DateTime.UtcNow,
            BalanceDayUtc = DateTime.UtcNow.Date,
            SourceAddressId = 10,
            DestinationAddressId = 20,
            SourceSurplusKwhAtWorkflow = 10,
            DestinationDeficitKwhAtWorkflow = 8,
            AmountKwh = 6,
            TriggerType = (int)TriggerType.Manual,
            Status = (int)TransferStatus.Approved,
            SettlementMode = 0,
            AppliedDistributionMode = 0
        });

        db.DailyEnergyBalances.AddRange(
            new DailyEnergyBalance
            {
                AddressId = 10,
                Day = DateTime.UtcNow.Date,
                ProducedKwh = 0,
                ConsumedKwh = 0,
                SurplusKwh = 4,
                DeficitKwh = 0,
                CalculatedAtUtc = DateTime.UtcNow,
                Status = "Computed",
                NetKwh = 4,
                NetPerAddressKwh = 4
            },
            new DailyEnergyBalance
            {
                AddressId = 20,
                Day = DateTime.UtcNow.Date,
                ProducedKwh = 0,
                ConsumedKwh = 5,
                SurplusKwh = 0,
                DeficitKwh = 5,
                CalculatedAtUtc = DateTime.UtcNow,
                Status = "Computed",
                NetKwh = -5,
                NetPerAddressKwh = -5
            });

        await db.SaveChangesAsync();

        TransferExecutionRequest? capturedRequest = null;
        var adapter = new Mock<ITransferExecutionAdapter>();
        adapter.Setup(a => a.ExecuteAsync(It.IsAny<TransferExecutionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TransferExecutionRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new TransferExecutionResult
            {
                Success = true,
                ExternalReference = "EXT-123",
                ExecutedAtUtc = DateTime.UtcNow
            });

        var sut = new TransferExecutionService(db, adapter.Object, NullLogger<TransferExecutionService>.Instance);

        await sut.ExecuteAsync(1, "tester", "done", CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal(4m, capturedRequest!.AmountKwh);

        var workflow = await db.TransferWorkflows.SingleAsync(x => x.Id == 1);
        Assert.Equal((int)TransferStatus.Executed, workflow.Status);
        Assert.Equal(4m, workflow.AmountAtExecutionKwh);
        Assert.Equal(4m, workflow.SourceSurplusKwhAtExecution);
        Assert.Equal(5m, workflow.DestinationDeficitKwhAtExecution);

        var ledger = await db.TransferLedgerEntries.SingleAsync(x => x.TransferWorkflowId == 1);
        Assert.Equal(4m, ledger.AmountKwh);
        Assert.Equal("EXT-123", ledger.ExecutionReference);
        Assert.Equal("done", ledger.Notes);

        var history = await db.TransferWorkflowStatusHistory.SingleAsync(x => x.TransferWorkflowId == 1);
        Assert.Equal((int)TransferStatus.Executed, history.ToStatus);
        Assert.Contains("ExternalReference=EXT-123", history.Note);
        Assert.Contains("UserNote=done", history.Note);
    }
}
