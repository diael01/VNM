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
    private readonly Mock<IMapper> _mapper = new();

    [Fact]
    public async Task CreateAsync_ForcesEntityIdToZero_BeforeAdd()
    {
        var dto = new TransferRuleDto
        {
            Id = 999,
            SourceAddressId = 1,
            DestinationAddressId = 2,
            IsEnabled = true,
            Priority = 1,
            DistributionMode = 0,
            MaxDailyKwh = null,
            WeightPercent = null,
        };

        var mappedEntity = new TransferRule
        {
            Id = 999,
            SourceAddressId = 1,
            DestinationAddressId = 2,
            IsEnabled = true,
            Priority = 1,
            DistributionMode = 0,
            MaxDailyKwh = null,
            WeightPercent = null,
        };

        TransferRule? addedEntity = null;

        _mapper.Setup(m => m.Map<TransferRule>(dto)).Returns(mappedEntity);
        _repo.Setup(r => r.AddAsync(It.IsAny<TransferRule>(), default))
            .Callback<TransferRule, CancellationToken>((e, _) => addedEntity = e)
            .ReturnsAsync((TransferRule e, CancellationToken _) => e);
        _mapper.Setup(m => m.Map<TransferRuleDto>(It.IsAny<TransferRule>()))
            .Returns((TransferRule src) => new TransferRuleDto
            {
                Id = src.Id,
                SourceAddressId = src.SourceAddressId,
                DestinationAddressId = src.DestinationAddressId,
                IsEnabled = src.IsEnabled,
                Priority = src.Priority,
                DistributionMode = src.DistributionMode,
                MaxDailyKwh = src.MaxDailyKwh,
                WeightPercent = src.WeightPercent,
            });

        var sut = new TransferRuleService(_repo.Object, _mapper.Object);

        var created = await sut.CreateAsync(dto);

        Assert.NotNull(addedEntity);
        Assert.Equal(0, addedEntity!.Id);
        Assert.Equal(0, created.Id);
    }

    [Fact]
    public async Task UpdateAsync_UsesRouteId_ForEntityId()
    {
        var routeId = 42;
        var dto = new TransferRuleDto
        {
            Id = 10,
            SourceAddressId = 1,
            DestinationAddressId = 2,
            IsEnabled = true,
            Priority = 1,
            DistributionMode = 1,
            MaxDailyKwh = 5,
            WeightPercent = null,
        };

        var mappedEntity = new TransferRule
        {
            Id = dto.Id,
            SourceAddressId = dto.SourceAddressId,
            DestinationAddressId = dto.DestinationAddressId,
            IsEnabled = dto.IsEnabled,
            Priority = dto.Priority,
            DistributionMode = dto.DistributionMode,
            MaxDailyKwh = dto.MaxDailyKwh,
            WeightPercent = dto.WeightPercent,
        };

        TransferRule? updatedEntity = null;

        _mapper.Setup(m => m.Map<TransferRule>(dto)).Returns(mappedEntity);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<TransferRule>(), default))
            .Callback<TransferRule, CancellationToken>((e, _) => updatedEntity = e)
            .ReturnsAsync((TransferRule e, CancellationToken _) => e);
        _mapper.Setup(m => m.Map<TransferRuleDto>(It.IsAny<TransferRule>()))
            .Returns((TransferRule src) => new TransferRuleDto
            {
                Id = src.Id,
                SourceAddressId = src.SourceAddressId,
                DestinationAddressId = src.DestinationAddressId,
                IsEnabled = src.IsEnabled,
                Priority = src.Priority,
                DistributionMode = src.DistributionMode,
                MaxDailyKwh = src.MaxDailyKwh,
                WeightPercent = src.WeightPercent,
            });

        var sut = new TransferRuleService(_repo.Object, _mapper.Object);

        var updated = await sut.UpdateAsync(routeId, dto);

        Assert.NotNull(updatedEntity);
        Assert.Equal(routeId, updatedEntity!.Id);
        Assert.Equal(routeId, updated.Id);
    }
}
