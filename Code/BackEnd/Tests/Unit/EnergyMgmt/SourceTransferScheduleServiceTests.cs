using AutoMapper;
using Infrastructure.DTOs;
using Infrastructure.Enums;
using Moq;
using Repositories.CRUD.Repositories;
using Repositories.Models;
using Services.Profiles;
using Services.Transfers;
using Xunit;

namespace Tests.Transfers;

public class SourceTransferScheduleServiceTests
{
    private readonly Mock<ISourceTransferScheduleRepository> _repo = new();
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<SourceTransferPolicyProfile>()).CreateMapper();

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedDto_OrNull()
    {
        var entity = new SourceTransferSchedule
        {
            Id = 7,
            SourceTransferPolicyId = 1,
            IsEnabled = true,
            ScheduleType = (int)ScheduleType.Daily,
            ExecutionMode = (int)ExecutionMode.PlanOnly,
            StartDateUtc = DateTime.UtcNow
        };

        _repo.Setup(r => r.GetByIdAsync(7, default)).ReturnsAsync(entity);
        _repo.Setup(r => r.GetByIdAsync(404, default)).ReturnsAsync((SourceTransferSchedule?)null);

        var sut = new SourceTransferScheduleService(_repo.Object, _mapper);

        var found = await sut.GetByIdAsync(7);
        var missing = await sut.GetByIdAsync(404);

        Assert.NotNull(found);
        Assert.Equal(7, found!.Id);
        Assert.Null(missing);
    }

    [Fact]
    public async Task CreateAsync_Interval_ComputesNextRun_WhenMissing()
    {
        SourceTransferSchedule? added = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<SourceTransferSchedule>(), default))
            .Callback<SourceTransferSchedule, CancellationToken>((e, _) => added = e)
            .ReturnsAsync((SourceTransferSchedule e, CancellationToken _) => e);

        var dto = new SourceTransferScheduleDto
        {
            Id = 99,
            SourceTransferPolicyId = 1,
            IsEnabled = true,
            ScheduleType = (int)ScheduleType.Interval,
            ExecutionMode = (int)ExecutionMode.PlanOnly,
            StartDateUtc = DateTime.UtcNow,
            RepeatEveryValue = 5,
            RepeatEveryUnit = (int)RepeatEveryUnit.Minutes
        };

        var before = DateTime.UtcNow;
        var sut = new SourceTransferScheduleService(_repo.Object, _mapper);
        var created = await sut.CreateAsync(dto);
        var after = DateTime.UtcNow;

        Assert.NotNull(added);
        Assert.Equal(0, added!.Id);
        Assert.Null(added.TimeOfDayUtc);
        Assert.NotNull(added.NextRunUtc);
        Assert.InRange(added.NextRunUtc!.Value, before.AddMinutes(5), after.AddMinutes(5).AddSeconds(1));
        Assert.NotNull(created.NextRunUtc);
    }

    [Fact]
    public async Task CreateAsync_Weekly_WithoutDayOfWeek_Throws()
    {
        var dto = new SourceTransferScheduleDto
        {
            SourceTransferPolicyId = 1,
            IsEnabled = true,
            ScheduleType = (int)ScheduleType.Weekly,
            ExecutionMode = (int)ExecutionMode.PlanOnly,
            StartDateUtc = DateTime.UtcNow,
            TimeOfDayUtc = TimeSpan.FromHours(8)
        };

        var sut = new SourceTransferScheduleService(_repo.Object, _mapper);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(dto));
        Assert.Contains("Weekly schedules must define DayOfWeek", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(500, default)).ReturnsAsync((SourceTransferSchedule?)null);
        var sut = new SourceTransferScheduleService(_repo.Object, _mapper);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.UpdateAsync(500, new SourceTransferScheduleDto()));
        Assert.Contains("SourceTransferSchedule 500", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_DisabledSchedule_DoesNotAutoComputeNextRun()
    {
        var existing = new SourceTransferSchedule
        {
            Id = 12,
            SourceTransferPolicyId = 1,
            IsEnabled = true,
            ScheduleType = (int)ScheduleType.Daily,
            ExecutionMode = (int)ExecutionMode.PlanOnly,
            StartDateUtc = DateTime.UtcNow.AddDays(-1),
            NextRunUtc = DateTime.UtcNow.AddDays(1)
        };

        _repo.Setup(r => r.GetByIdAsync(12, default)).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(existing, default)).ReturnsAsync(existing);

        var dto = new SourceTransferScheduleDto
        {
            SourceTransferPolicyId = 2,
            IsEnabled = false,
            ScheduleType = (int)ScheduleType.Daily,
            ExecutionMode = (int)ExecutionMode.PlanAndApprove,
            StartDateUtc = DateTime.UtcNow.Date,
            TimeOfDayUtc = TimeSpan.FromHours(6),
            NextRunUtc = null
        };

        var sut = new SourceTransferScheduleService(_repo.Object, _mapper);
        var updated = await sut.UpdateAsync(12, dto);

        Assert.False(existing.IsEnabled);
        Assert.Equal(2, existing.SourceTransferPolicyId);
        Assert.Null(existing.NextRunUtc);
        Assert.Null(updated.NextRunUtc);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.DeleteAsync(13, default)).ReturnsAsync(true);
        var sut = new SourceTransferScheduleService(_repo.Object, _mapper);

        var deleted = await sut.DeleteAsync(13);

        Assert.True(deleted);
    }
}
