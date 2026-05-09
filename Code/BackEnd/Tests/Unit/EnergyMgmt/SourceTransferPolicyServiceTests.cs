using AutoMapper;
using Infrastructure.DTOs;
using Moq;
using Repositories.CRUD.Repositories;
using Repositories.Models;
using Services.Profiles;
using Services.Transfers;
using Xunit;

namespace Tests.Transfers;

public class SourceTransferPolicyServiceTests
{
    private readonly Mock<ISourceTransferPolicyRepository> _policyRepo = new();
    private readonly Mock<ISourceTransferScheduleRepository> _scheduleRepo = new();
    private readonly Mock<ITransferRuleRepository> _ruleRepo = new();
    private readonly Mock<ITransferWorkflowRepository> _workflowRepo = new();
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<SourceTransferPolicyProfile>()).CreateMapper();

    [Fact]
    public async Task GetAllAndGetById_MapAndReturnResults()
    {
        var policy = new SourceTransferPolicy
        {
            Id = 1,
            SourceAddressId = 10,
            DistributionMode = 2,
            IsEnabled = true,
            DestinationTransferRules = [new DestinationTransferRule(), new DestinationTransferRule()],
            SourceTransferSchedules = [new SourceTransferSchedule()]
        };

        _policyRepo.Setup(r => r.GetAllWithCountsAsync(default)).ReturnsAsync([policy]);
        _policyRepo.Setup(r => r.GetByIdWithChildrenAsync(1, default)).ReturnsAsync(policy);
        _policyRepo.Setup(r => r.GetByIdWithChildrenAsync(404, default)).ReturnsAsync((SourceTransferPolicy?)null);

        var sut = new SourceTransferPolicyService(_policyRepo.Object, _scheduleRepo.Object, _ruleRepo.Object, _workflowRepo.Object, _mapper);

        var all = await sut.GetAllAsync();
        var byId = await sut.GetByIdAsync(1);
        var missing = await sut.GetByIdAsync(404);

        Assert.Single(all);
        Assert.Equal(2, all[0].DestinationRulesCount);
        Assert.NotNull(byId);
        Assert.Equal(1, byId!.SchedulesCount);
        Assert.Null(missing);
    }

    [Fact]
    public async Task CreateAsync_ForcesIdToZero_AndUpdateAsync_PatchesMutableFields()
    {
        var createDto = new SourceTransferPolicyDto
        {
            Id = 99,
            SourceAddressId = 7,
            DistributionMode = 1,
            IsEnabled = true
        };

        SourceTransferPolicy? addedEntity = null;
        var addedIdAtCallTime = -1;
        _policyRepo.Setup(r => r.AddAsync(It.IsAny<SourceTransferPolicy>(), default))
            .Callback<SourceTransferPolicy, CancellationToken>((e, _) =>
            {
                addedEntity = e;
                addedIdAtCallTime = e.Id;
            })
            .ReturnsAsync((SourceTransferPolicy e, CancellationToken _) =>
            {
                e.Id = 123;
                return e;
            });

        var existing = new SourceTransferPolicy
        {
            Id = 123,
            SourceAddressId = 1,
            DistributionMode = 0,
            IsEnabled = false
        };

        _policyRepo.Setup(r => r.GetByIdAsync(123, default)).ReturnsAsync(existing);
        _policyRepo.Setup(r => r.UpdateAsync(existing, default)).ReturnsAsync(existing);

        var sut = new SourceTransferPolicyService(_policyRepo.Object, _scheduleRepo.Object, _ruleRepo.Object, _workflowRepo.Object, _mapper);

        var created = await sut.CreateAsync(createDto);

        Assert.NotNull(addedEntity);
        Assert.Equal(0, addedIdAtCallTime);
        Assert.Equal(123, created.Id);

        var updateDto = new SourceTransferPolicyDto
        {
            SourceAddressId = 42,
            DistributionMode = 2,
            IsEnabled = true
        };

        var updated = await sut.UpdateAsync(123, updateDto);

        Assert.Equal(42, existing.SourceAddressId);
        Assert.Equal(2, existing.DistributionMode);
        Assert.True(existing.IsEnabled);
        Assert.Equal(42, updated.SourceAddressId);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsKeyNotFound()
    {
        _policyRepo.Setup(r => r.GetByIdAsync(404, default)).ReturnsAsync((SourceTransferPolicy?)null);
        var sut = new SourceTransferPolicyService(_policyRepo.Object, _scheduleRepo.Object, _ruleRepo.Object, _workflowRepo.Object, _mapper);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.UpdateAsync(404, new SourceTransferPolicyDto()));
        Assert.Contains("SourceTransferPolicy 404", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ReturnsFalse()
    {
        _policyRepo.Setup(r => r.GetByIdAsync(7, default)).ReturnsAsync((SourceTransferPolicy?)null);
        var sut = new SourceTransferPolicyService(_policyRepo.Object, _scheduleRepo.Object, _ruleRepo.Object, _workflowRepo.Object, _mapper);

        var deleted = await sut.DeleteAsync(7);

        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAsync_UnlinksWorkflowsAndDeletesChildren()
    {
        _policyRepo.Setup(r => r.GetByIdAsync(5, default)).ReturnsAsync(new SourceTransferPolicy { Id = 5 });

        var ruleA = new DestinationTransferRule { Id = 11, SourceTransferPolicyId = 5 };
        var ruleB = new DestinationTransferRule { Id = 12, SourceTransferPolicyId = 5 };
        _ruleRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DestinationTransferRule, bool>>>(), default))
            .ReturnsAsync([ruleA, ruleB]);

        var linked = new TransferWorkflow { Id = 100, DestinationTransferRuleId = 11 };
        _workflowRepo.SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TransferWorkflow, bool>>>(), default))
            .ReturnsAsync([linked])
            .ReturnsAsync([]);

        var updatedWorkflows = new List<TransferWorkflow>();
        _workflowRepo.Setup(r => r.UpdateAsync(It.IsAny<TransferWorkflow>(), default))
            .Callback<TransferWorkflow, CancellationToken>((wf, _) => updatedWorkflows.Add(wf))
            .ReturnsAsync((TransferWorkflow wf, CancellationToken _) => wf);

        var scheduleA = new SourceTransferSchedule { Id = 21, SourceTransferPolicyId = 5 };
        var scheduleB = new SourceTransferSchedule { Id = 22, SourceTransferPolicyId = 5 };
        _scheduleRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SourceTransferSchedule, bool>>>(), default))
            .ReturnsAsync([scheduleA, scheduleB]);

        _ruleRepo.Setup(r => r.DeleteAsync(It.IsAny<object>(), default)).ReturnsAsync(true);
        _scheduleRepo.Setup(r => r.DeleteAsync(It.IsAny<object>(), default)).ReturnsAsync(true);
        _policyRepo.Setup(r => r.DeleteAsync(5, default)).ReturnsAsync(true);

        var sut = new SourceTransferPolicyService(_policyRepo.Object, _scheduleRepo.Object, _ruleRepo.Object, _workflowRepo.Object, _mapper);

        var deleted = await sut.DeleteAsync(5);

        Assert.True(deleted);
        Assert.Single(updatedWorkflows);
        Assert.Null(updatedWorkflows[0].DestinationTransferRuleId);
        _ruleRepo.Verify(r => r.DeleteAsync(11, default), Times.Once);
        _ruleRepo.Verify(r => r.DeleteAsync(12, default), Times.Once);
        _scheduleRepo.Verify(r => r.DeleteAsync(21, default), Times.Once);
        _scheduleRepo.Verify(r => r.DeleteAsync(22, default), Times.Once);
    }

    [Fact]
    public async Task GetRulesAndSchedules_ReturnsEmptyWhenPolicyMissing()
    {
        _policyRepo.Setup(r => r.GetByIdWithChildrenAsync(88, default)).ReturnsAsync((SourceTransferPolicy?)null);
        var sut = new SourceTransferPolicyService(_policyRepo.Object, _scheduleRepo.Object, _ruleRepo.Object, _workflowRepo.Object, _mapper);

        var rules = await sut.GetRulesAsync(88);
        var schedules = await sut.GetSchedulesAsync(88);

        Assert.Empty(rules);
        Assert.Empty(schedules);
    }
}
