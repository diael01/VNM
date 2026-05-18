using AutoMapper;
using EnergyManagement.Services.Providers;
using Infrastructure.Enums;
using Infrastructure.DTOs;
using Microsoft.EntityFrameworkCore;
using Repositories.CRUD.Repositories;
using Repositories.Models;
using Moq;
using Services.Profiles;
using Services.Transfers;
using System.Reflection;
using Xunit;

namespace Tests.Transfers;

public class TransitionWorkflowServiceTests
{
    private static VnmDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<VnmDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new VnmDbContext(options);
    }

    private static TransitionWorkflowService CreateSut(VnmDbContext db)
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<TransferWorkflowProfile>()).CreateMapper();
        var repo = new TransferWorkflowRepository(db);
        var providerSettlementService = new Mock<IProviderSettlementService>();
        providerSettlementService
            .Setup(x => x.SettleWorkflowAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderSettlement());

        return new TransitionWorkflowService(repo, mapper, db, providerSettlementService.Object);
    }

    private static (TransitionWorkflowService Sut, Mock<IProviderSettlementService> ProviderSettlementServiceMock) CreateSutWithMocks(VnmDbContext db)
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<TransferWorkflowProfile>()).CreateMapper();
        var repo = new TransferWorkflowRepository(db);
        var providerSettlementService = new Mock<IProviderSettlementService>();
        providerSettlementService
            .Setup(x => x.SettleWorkflowAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderSettlement());

        return (new TransitionWorkflowService(repo, mapper, db, providerSettlementService.Object), providerSettlementService);
    }

    [Fact]
    public async Task GetAllAndGetById_ReturnExpectedDtos()
    {
        await using var db = CreateContext("TransitionWorkflow_GetAll");
        db.TransferWorkflows.AddRange(
            NewWorkflow(1, TransferStatus.Planned),
            NewWorkflow(2, TransferStatus.Approved));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var all = await sut.GetAllAsync();
        var byId = await sut.GetByIdAsync(2);
        var missing = await sut.GetByIdAsync(404);

        Assert.Equal(2, all.Count);
        Assert.NotNull(byId);
        Assert.Equal(2, byId!.Id);
        Assert.Null(missing);
    }

    [Fact]
    public async Task GetAllHistoryAndGetHistory_ReturnOrderedDtos()
    {
        await using var db = CreateContext("TransitionWorkflow_GetHistory");
        db.TransferWorkflows.Add(NewWorkflow(1, TransferStatus.Planned));
        db.TransferWorkflowStatusHistory.Add(new TransferWorkflowStatusHistory
        {
            TransferWorkflowId = 1,
            FromStatus = (int)TransferStatus.Planned,
            ToStatus = (int)TransferStatus.Approved,
            Note = "first",
            CreatedBy = "user1"
        });
        await db.SaveChangesAsync();

        db.TransferWorkflowStatusHistory.Add(new TransferWorkflowStatusHistory
        {
            TransferWorkflowId = 1,
            FromStatus = (int)TransferStatus.Approved,
            ToStatus = (int)TransferStatus.Executed,
            Note = "second",
            CreatedBy = "user2"
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var allHistory = await sut.GetAllHistoryAsync();
        var workflowHistory = await sut.GetHistoryAsync(1);

        Assert.Equal(2, allHistory.Count);
        Assert.Equal("second", allHistory[0].Note);
        Assert.Equal("first", allHistory[1].Note);

        Assert.Equal(2, workflowHistory.Count);
        Assert.Equal("first", workflowHistory[0].Note);
        Assert.Equal("second", workflowHistory[1].Note);
    }

    [Fact]
    public async Task ApproveAsync_TransitionsAndWritesHistory()
    {
        await using var db = CreateContext("TransitionWorkflow_Approve");
        db.TransferWorkflows.Add(NewWorkflow(1, TransferStatus.Planned));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var dto = await sut.ApproveAsync(1, "approved by reviewer");

        Assert.Equal((int)TransferStatus.Approved, dto.Status);

        var history = await db.TransferWorkflowStatusHistory.SingleAsync(x => x.TransferWorkflowId == 1);
        Assert.Equal((int)TransferStatus.Planned, history.FromStatus);
        Assert.Equal((int)TransferStatus.Approved, history.ToStatus);
        Assert.Equal("approved by reviewer", history.Note);
    }

    [Fact]
    public async Task RejectAsync_WritesDefaultDiscontinuedNoteToWorkflowAndHistory()
    {
        await using var db = CreateContext("TransitionWorkflow_Reject_DefaultNote");
        db.TransferWorkflows.Add(NewWorkflow(1, TransferStatus.Planned));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var dto = await sut.RejectAsync(1);

        Assert.Equal((int)TransferStatus.Discontinued, dto.Status);

        var workflow = await db.TransferWorkflows.SingleAsync(x => x.Id == 1);
        Assert.Equal("Rejected by user", workflow.Notes);

        var history = await db.TransferWorkflowStatusHistory.SingleAsync(x => x.TransferWorkflowId == 1);
        Assert.Equal((int)TransferStatus.Planned, history.FromStatus);
        Assert.Equal((int)TransferStatus.Discontinued, history.ToStatus);
        Assert.Equal("Rejected by user", history.Note);
    }

    [Fact]
    public async Task RejectAsync_AppendsUserNoteAndPersistsToWorkflowAndHistory()
    {
        await using var db = CreateContext("TransitionWorkflow_Reject_CustomNote");
        db.TransferWorkflows.Add(NewWorkflow(1, TransferStatus.Approved));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var dto = await sut.RejectAsync(1, "manual stop");

        Assert.Equal((int)TransferStatus.Discontinued, dto.Status);

        var workflow = await db.TransferWorkflows.SingleAsync(x => x.Id == 1);
        Assert.Equal("Rejected by user. manual stop", workflow.Notes);

        var history = await db.TransferWorkflowStatusHistory.SingleAsync(x => x.TransferWorkflowId == 1);
        Assert.Equal((int)TransferStatus.Approved, history.FromStatus);
        Assert.Equal((int)TransferStatus.Discontinued, history.ToStatus);
        Assert.Equal("Rejected by user. manual stop", history.Note);
    }

    [Fact]
    public async Task Transition_WhenMissing_Throws()
    {
        await using var db = CreateContext("TransitionWorkflow_Missing");
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ApproveAsync(999));
        Assert.Contains("Transfer workflow 999 was not found", ex.Message);
    }

    [Fact]
    public async Task Transition_WhenInvalid_Throws()
    {
        await using var db = CreateContext("TransitionWorkflow_Invalid");
        db.TransferWorkflows.Add(NewWorkflow(1, TransferStatus.Planned));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SettleAsync(1));
        Assert.Contains("Invalid status transition", ex.Message);
    }

    [Fact]
    public async Task Transition_WhenAlreadyInTargetStatus_ReturnsWithoutHistoryWrite()
    {
        await using var db = CreateContext("TransitionWorkflow_SameStatus");
        db.TransferWorkflows.Add(NewWorkflow(1, TransferStatus.Approved));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var dto = await sut.ApproveAsync(1, "noop");

        Assert.Equal((int)TransferStatus.Approved, dto.Status);
        Assert.Empty(await db.TransferWorkflowStatusHistory.ToListAsync());
    }

    [Fact]
    public async Task ExecuteAsync_ComputesExecutionFields()
    {
        await using var db = CreateContext("TransitionWorkflow_Execute");
        db.TransferWorkflows.Add(new TransferWorkflow
        {
            Id = 1,
            EffectiveAtUtc = DateTime.UtcNow,
            BalanceDayUtc = DateTime.UtcNow.Date,
            SourceAddressId = 10,
            DestinationAddressId = 20,
            SourceSurplusKwhAtWorkflow = 10,
            DestinationDeficitKwhAtWorkflow = 7,
            AmountKwh = 4,
            TriggerType = (int)TriggerType.Manual,
            Status = (int)TransferStatus.Approved,
            SettlementMode = 0,
            AppliedDistributionMode = 0
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var dto = await sut.ExecuteAsync(1, "executed now");

        Assert.Equal((int)TransferStatus.Executed, dto.Status);

        var workflow = await db.TransferWorkflows.SingleAsync(x => x.Id == 1);
        Assert.Equal(6m, workflow.SourceSurplusKwhAtExecution);
        Assert.Equal(3m, workflow.DestinationDeficitKwhAtExecution);
        Assert.Equal(4m, workflow.AmountAtExecutionKwh);

        var history = await db.TransferWorkflowStatusHistory.SingleAsync(x => x.TransferWorkflowId == 1);
        Assert.Equal((int)TransferStatus.Approved, history.FromStatus);
        Assert.Equal((int)TransferStatus.Executed, history.ToStatus);
    }

    [Fact]
    public async Task SettleAsync_WhenAlreadySettled_ReturnsWithoutCallingProviderService()
    {
        await using var db = CreateContext("TransitionWorkflow_Settle_AlreadySettled");
        db.TransferWorkflows.Add(NewWorkflow(1, TransferStatus.Settled));
        await db.SaveChangesAsync();

        var (sut, providerMock) = CreateSutWithMocks(db);

        var dto = await sut.SettleAsync(1);

        Assert.Equal((int)TransferStatus.Settled, dto.Status);
        providerMock.Verify(x => x.SettleWorkflowAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SettleAsync_WhenExecuted_UsesDefaultSettleNote()
    {
        await using var db = CreateContext("TransitionWorkflow_Settle_DefaultNote");
        db.TransferWorkflows.Add(NewWorkflow(1, TransferStatus.Executed));
        await db.SaveChangesAsync();

        var (sut, providerMock) = CreateSutWithMocks(db);

        _ = await sut.SettleAsync(1);

        providerMock.Verify(
            x => x.SettleWorkflowAsync(
                1,
                "Workflow has been settled",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void BuildDiscontinuedNote_CoversAllReasonBranches()
    {
        var method = typeof(TransitionWorkflowService).GetMethod(
            "BuildDiscontinuedNote",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var rejectedDefault = (string)method!.Invoke(null, new object?[] { DiscontinuedReason.UserRejected, null })!;
        var rejectedWithNote = (string)method.Invoke(null, new object?[] { DiscontinuedReason.UserRejected, "x" })!;
        var failedWithDetail = (string)method.Invoke(null, new object?[] { DiscontinuedReason.ExecutionFailed, "boom" })!;
        var expiredBeforeApproval = (string)method.Invoke(null, new object?[] { DiscontinuedReason.ExpiredBeforeApproval, null })!;
        var expiredBeforeExecution = (string)method.Invoke(null, new object?[] { DiscontinuedReason.ExpiredBeforeExecution, null })!;

        Assert.Equal("Rejected by user", rejectedDefault);
        Assert.Equal("Rejected by user. x", rejectedWithNote);
        Assert.Equal("Failed while performing ExecuteWorkflowAsync: boom", failedWithDetail);
        Assert.Contains("Expired: workflow was still Planned", expiredBeforeApproval);
        Assert.Contains("Expired: workflow was Approved", expiredBeforeExecution);
    }

    [Fact]
    public async Task Transition_FromDiscontinued_ThrowsForAnyFurtherTransition()
    {
        await using var db = CreateContext("TransitionWorkflow_FromDiscontinued_Invalid");
        db.TransferWorkflows.Add(NewWorkflow(1, TransferStatus.Discontinued));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(1));
        Assert.Contains("Invalid status transition", ex.Message);
    }

    [Fact]
    public async Task Transition_FromSettled_ThrowsForAnyFurtherTransition()
    {
        await using var db = CreateContext("TransitionWorkflow_FromSettled_Invalid");
        db.TransferWorkflows.Add(NewWorkflow(1, TransferStatus.Settled));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ApproveAsync(1));
        Assert.Contains("Invalid status transition", ex.Message);
    }

    [Fact]
    public async Task Transition_FromUnknownStatus_Throws()
    {
        await using var db = CreateContext("TransitionWorkflow_FromUnknown_Invalid");
        var wf = NewWorkflow(1, TransferStatus.Planned);
        wf.Status = 999;
        db.TransferWorkflows.Add(wf);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ApproveAsync(1));
        Assert.Contains("Invalid status transition", ex.Message);
    }

    private static TransferWorkflow NewWorkflow(int id, TransferStatus status)
    {
        return new TransferWorkflow
        {
            Id = id,
            EffectiveAtUtc = DateTime.UtcNow,
            BalanceDayUtc = DateTime.UtcNow.Date,
            SourceAddressId = 10,
            DestinationAddressId = 20,
            SourceSurplusKwhAtWorkflow = 10,
            DestinationDeficitKwhAtWorkflow = 8,
            AmountKwh = 5,
            TriggerType = (int)TriggerType.Manual,
            Status = (int)status,
            SettlementMode = 0,
            AppliedDistributionMode = 0
        };
    }
}
