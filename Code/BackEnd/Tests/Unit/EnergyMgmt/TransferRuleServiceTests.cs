using AutoMapper;
using Infrastructure.DTOs;
using Moq;
using Repositories.CRUD.Repositories;
using Repositories.Models;
using Services.Transfers;
using Xunit;

namespace Tests.Transfers;

public class TransferRuleServiceTests
{
    private readonly Mock<ITransferRuleRepository> _repo = new();
    private readonly Mock<ISourceTransferPolicyRepository> _sourcePolicyRepo = new();
    private readonly Mock<ITransferWorkflowRepository> _workflowRepo = new();
    private readonly Mock<IMapper> _mapper = new();

    [Fact]
    public async Task CreateAsync_ForcesEntityIdToZero_BeforeAdd()
    {
        var dto = new TransferRuleDto
        {
            Id = 999,
            SourceTransferPolicyId = 1,
            DestinationAddressId = 2,
            IsEnabled = true,
            Priority = 1,
            DistributionMode = 0,
            MaxDailyKwh = null,
            WeightPercent = null,
        };

        var mappedEntity = new DestinationTransferRule
        {
            Id = 999,
            SourceTransferPolicyId = 1,
            DestinationAddressId = 2,
            IsEnabled = true,
            Priority = 1,
            DistributionMode = 0,
            MaxDailyKwh = null,
            WeightPercent = null,
        };

        DestinationTransferRule? addedEntity = null;
        var sourcePolicy = new SourceTransferPolicy
        {
            Id = dto.SourceTransferPolicyId,
            SourceAddressId = 99
        };

        _mapper.Setup(m => m.Map<DestinationTransferRule>(dto)).Returns(mappedEntity);
        _sourcePolicyRepo.Setup(r => r.GetByIdAsync(dto.SourceTransferPolicyId, default)).ReturnsAsync(sourcePolicy);
        _repo.Setup(r => r.AddAsync(It.IsAny<DestinationTransferRule>(), default))
            .Callback<DestinationTransferRule, CancellationToken>((e, _) => addedEntity = e)
            .ReturnsAsync((DestinationTransferRule e, CancellationToken _) => e);
        _mapper.Setup(m => m.Map<TransferRuleDto>(It.IsAny<DestinationTransferRule>()))
            .Returns((DestinationTransferRule src) => new TransferRuleDto
            {
                Id = src.Id,
                SourceTransferPolicyId = src.SourceTransferPolicyId,
                DestinationAddressId = src.DestinationAddressId,
                IsEnabled = src.IsEnabled,
                Priority = src.Priority,
                DistributionMode = src.DistributionMode,
                MaxDailyKwh = src.MaxDailyKwh,
                WeightPercent = src.WeightPercent,
            });

        var sut = new TransferRuleService(_repo.Object, _sourcePolicyRepo.Object, _workflowRepo.Object, _mapper.Object);

        var created = await sut.CreateAsync(dto);
 
        Assert.NotNull(addedEntity);
        Assert.Equal(0, addedEntity!.Id);
        Assert.Equal(0, created.Id);
    }

    [Fact]
    public async Task UpdateAsync_PatchesExistingEntity_AndPersists()
    {
        var routeId = 42;
        var dto = new TransferRuleDto
        {
            Id = 10,
            SourceTransferPolicyId = 1,
            DestinationAddressId = 2,
            IsEnabled = true,
            Priority = 1,
            DistributionMode = 1,
            MaxDailyKwh = 5,
            WeightPercent = null,
        };

        var existing = new DestinationTransferRule
        {
            Id = routeId,
            SourceTransferPolicyId = 99,
            DestinationAddressId = 77,
            IsEnabled = false,
            Priority = 9,
            DistributionMode = 0,
            MaxDailyKwh = 1,
            WeightPercent = 2,
        };

        DestinationTransferRule? updatedEntity = null;
        var sourcePolicy = new SourceTransferPolicy
        {
            Id = dto.SourceTransferPolicyId,
            SourceAddressId = 99
        };

        _repo.Setup(r => r.GetByIdAsync(routeId, default)).ReturnsAsync(existing);
        _sourcePolicyRepo.Setup(r => r.GetByIdAsync(dto.SourceTransferPolicyId, default)).ReturnsAsync(sourcePolicy);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<DestinationTransferRule>(), default))
            .Callback<DestinationTransferRule, CancellationToken>((e, _) => updatedEntity = e)
            .ReturnsAsync((DestinationTransferRule e, CancellationToken _) => e);
        _mapper.Setup(m => m.Map<TransferRuleDto>(It.IsAny<DestinationTransferRule>()))
            .Returns((DestinationTransferRule src) => new TransferRuleDto
            {
                Id = src.Id,
                SourceTransferPolicyId = src.SourceTransferPolicyId,
                DestinationAddressId = src.DestinationAddressId,
                IsEnabled = src.IsEnabled,
                Priority = src.Priority,
                DistributionMode = src.DistributionMode,
                MaxDailyKwh = src.MaxDailyKwh,
                WeightPercent = src.WeightPercent,
            });

        var sut = new TransferRuleService(_repo.Object, _sourcePolicyRepo.Object, _workflowRepo.Object, _mapper.Object);

        var updated = await sut.UpdateAsync(routeId, dto);

        Assert.NotNull(updatedEntity);
        Assert.Same(existing, updatedEntity);
        Assert.Equal(routeId, updatedEntity!.Id);
        Assert.Equal(dto.SourceTransferPolicyId, updatedEntity.SourceTransferPolicyId);
        Assert.Equal(dto.DestinationAddressId, updatedEntity.DestinationAddressId);
        Assert.Equal(dto.IsEnabled, updatedEntity.IsEnabled);
        Assert.Equal(dto.Priority, updatedEntity.Priority);
        Assert.Equal(dto.MaxDailyKwh, updatedEntity.MaxDailyKwh);
        Assert.Equal(dto.WeightPercent, updatedEntity.WeightPercent);
        Assert.Equal(routeId, updated.Id);
    }

    [Fact]
    public async Task GetAllAsync_AndGetByIdAsync_MapRepositoryResults()
    {
        var entity = new DestinationTransferRule
        {
            Id = 5,
            SourceTransferPolicyId = 1,
            DestinationAddressId = 9,
            IsEnabled = true,
            Priority = 2,
            DistributionMode = 1,
            MaxDailyKwh = 11,
            WeightPercent = 33
        };

        _repo.Setup(r => r.GetAllAsync(default)).ReturnsAsync([entity]);
        _repo.Setup(r => r.GetByIdAsync(5, default)).ReturnsAsync(entity);
        _repo.Setup(r => r.GetByIdAsync(999, default)).ReturnsAsync((DestinationTransferRule?)null);
        _mapper.Setup(m => m.Map<List<TransferRuleDto>>(It.IsAny<IEnumerable<DestinationTransferRule>>()))
            .Returns((IEnumerable<DestinationTransferRule> src) => src.Select(MapDto).ToList());
        _mapper.Setup(m => m.Map<TransferRuleDto>(It.IsAny<DestinationTransferRule>()))
            .Returns((DestinationTransferRule src) => MapDto(src));

        var sut = new TransferRuleService(_repo.Object, _sourcePolicyRepo.Object, _workflowRepo.Object, _mapper.Object);

        var all = await sut.GetAllAsync();
        var byId = await sut.GetByIdAsync(5);
        var missing = await sut.GetByIdAsync(999);

        Assert.Single(all);
        Assert.Equal(5, all[0].Id);
        Assert.NotNull(byId);
        Assert.Equal(9, byId!.DestinationAddressId);
        Assert.Null(missing);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenPolicyMissing()
    {
        var dto = new TransferRuleDto
        {
            SourceTransferPolicyId = 7,
            DestinationAddressId = 8
        };

        _sourcePolicyRepo.Setup(r => r.GetByIdAsync(dto.SourceTransferPolicyId, default))
            .ReturnsAsync((SourceTransferPolicy?)null);

        var sut = new TransferRuleService(_repo.Object, _sourcePolicyRepo.Object, _workflowRepo.Object, _mapper.Object);

        var error = await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.CreateAsync(dto));
        Assert.Contains("SourceTransferPolicy 7", error.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenSourceAndDestinationMatch()
    {
        var dto = new TransferRuleDto
        {
            SourceTransferPolicyId = 4,
            DestinationAddressId = 10
        };

        _sourcePolicyRepo.Setup(r => r.GetByIdAsync(dto.SourceTransferPolicyId, default))
            .ReturnsAsync(new SourceTransferPolicy { Id = 4, SourceAddressId = 10 });

        var sut = new TransferRuleService(_repo.Object, _sourcePolicyRepo.Object, _workflowRepo.Object, _mapper.Object);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(dto));
        Assert.Contains("Source and destination cannot be the same address", error.Message);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenRuleMissing()
    {
        _repo.Setup(r => r.GetByIdAsync(404, default)).ReturnsAsync((DestinationTransferRule?)null);

        var sut = new TransferRuleService(_repo.Object, _sourcePolicyRepo.Object, _workflowRepo.Object, _mapper.Object);

        var error = await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.UpdateAsync(404, new TransferRuleDto()));
        Assert.Contains("DestinationTransferRule 404", error.Message);
    }

    [Fact]
    public async Task DeleteAsync_UnlinksLinkedWorkflows_BeforeDeletingRule()
    {
        var workflowA = new TransferWorkflow { Id = 1, DestinationTransferRuleId = 42 };
        var workflowB = new TransferWorkflow { Id = 2, DestinationTransferRuleId = 42 };
        var updatedWorkflows = new List<TransferWorkflow>();

        _workflowRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TransferWorkflow, bool>>>(), default))
            .ReturnsAsync([workflowA, workflowB]);
        _workflowRepo.Setup(r => r.UpdateAsync(It.IsAny<TransferWorkflow>(), default))
            .Callback<TransferWorkflow, CancellationToken>((workflow, _) => updatedWorkflows.Add(workflow))
            .ReturnsAsync((TransferWorkflow workflow, CancellationToken _) => workflow);
        _repo.Setup(r => r.DeleteAsync(42, default)).ReturnsAsync(true);

        var sut = new TransferRuleService(_repo.Object, _sourcePolicyRepo.Object, _workflowRepo.Object, _mapper.Object);

        var deleted = await sut.DeleteAsync(42);

        Assert.True(deleted);
        Assert.Equal(2, updatedWorkflows.Count);
        Assert.All(updatedWorkflows, workflow => Assert.Null(workflow.DestinationTransferRuleId));
    }

    private static TransferRuleDto MapDto(DestinationTransferRule src)
    {
        return new TransferRuleDto
        {
            Id = src.Id,
            SourceTransferPolicyId = src.SourceTransferPolicyId,
            DestinationAddressId = src.DestinationAddressId,
            IsEnabled = src.IsEnabled,
            Priority = src.Priority,
            DistributionMode = src.DistributionMode,
            MaxDailyKwh = src.MaxDailyKwh,
            WeightPercent = src.WeightPercent,
        };
    }
}
