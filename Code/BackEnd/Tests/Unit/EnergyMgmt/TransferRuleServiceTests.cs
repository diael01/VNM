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

        _mapper.Setup(m => m.Map<DestinationTransferRule>(dto)).Returns(mappedEntity);
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

        var sut = new TransferRuleService(_repo.Object, _workflowRepo.Object, _mapper.Object);

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

        _repo.Setup(r => r.GetByIdAsync(routeId, default)).ReturnsAsync(existing);
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

        var sut = new TransferRuleService(_repo.Object, _workflowRepo.Object, _mapper.Object);

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
}
