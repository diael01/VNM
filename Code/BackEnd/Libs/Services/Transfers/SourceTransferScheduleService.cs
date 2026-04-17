using AutoMapper;
using Infrastructure.DTOs;
using Infrastructure.Enums;
using Repositories.CRUD.Repositories;
using Repositories.Models;

namespace Services.Transfers
{
    public interface ISourceTransferScheduleService
    {
        Task<SourceTransferScheduleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<SourceTransferScheduleDto> CreateAsync(SourceTransferScheduleDto dto, CancellationToken cancellationToken = default);
        Task<SourceTransferScheduleDto> UpdateAsync(int id, SourceTransferScheduleDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }

    public sealed class SourceTransferScheduleService : ISourceTransferScheduleService
    {
        private readonly ISourceTransferScheduleRepository _scheduleRepository;
        private readonly IMapper _mapper;

        public SourceTransferScheduleService(
            ISourceTransferScheduleRepository scheduleRepository,
            IMapper mapper)
        {
            _scheduleRepository = scheduleRepository;
            _mapper = mapper;
        }

        public async Task<SourceTransferScheduleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _scheduleRepository.GetByIdAsync(id, cancellationToken);
            return entity is null ? null : _mapper.Map<SourceTransferScheduleDto>(entity);
        }

        public async Task<SourceTransferScheduleDto> CreateAsync(SourceTransferScheduleDto dto, CancellationToken cancellationToken = default)
        {
            var entity = _mapper.Map<SourceTransferSchedule>(dto);
            entity.Id = 0;

            NormalizeSchedule(entity);

            if (!entity.NextRunUtc.HasValue)
                entity.NextRunUtc = CalculateNextRunUtc(entity, DateTime.UtcNow);

            var created = await _scheduleRepository.AddAsync(entity, cancellationToken);
            return _mapper.Map<SourceTransferScheduleDto>(created);
        }

        public async Task<SourceTransferScheduleDto> UpdateAsync(int id, SourceTransferScheduleDto dto, CancellationToken cancellationToken = default)
        {
            var existing = await _scheduleRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"SourceTransferSchedule {id} was not found.");

            existing.SourceTransferPolicyId = dto.SourceTransferPolicyId;
            existing.IsEnabled = dto.IsEnabled;
            existing.ScheduleType = dto.ScheduleType;
            existing.ExecutionMode = dto.ExecutionMode;
            existing.StartDateUtc = dto.StartDateUtc;
            existing.EndDateUtc = dto.EndDateUtc;
            existing.TimeOfDayUtc = dto.TimeOfDayUtc;
            existing.RepeatEveryValue = dto.RepeatEveryValue;
            existing.RepeatEveryUnit = dto.RepeatEveryUnit;
            existing.DayOfWeek = dto.DayOfWeek;
            existing.DayOfMonth = dto.DayOfMonth;

            // Preserve LastRunUtc unless caller explicitly sends it
            existing.LastRunUtc = dto.LastRunUtc;
            existing.NextRunUtc = dto.NextRunUtc;

            NormalizeSchedule(existing);

            if (!existing.NextRunUtc.HasValue && existing.IsEnabled)
                existing.NextRunUtc = CalculateNextRunUtc(existing, DateTime.UtcNow);

            var updated = await _scheduleRepository.UpdateAsync(existing, cancellationToken);
            return _mapper.Map<SourceTransferScheduleDto>(updated);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _scheduleRepository.DeleteAsync(id, cancellationToken);
        }

        private static void NormalizeSchedule(SourceTransferSchedule schedule)
        {
            var type = schedule.ScheduleTypeEnum;

            switch (type)
            {
                case ScheduleType.Interval:
                    schedule.TimeOfDayUtc = null;
                    schedule.DayOfWeek = null;
                    schedule.DayOfMonth = null;

                    if (!schedule.RepeatEveryValue.HasValue || schedule.RepeatEveryValue.Value <= 0)
                        throw new InvalidOperationException("Interval schedules must define RepeatEveryValue > 0.");

                    if (!schedule.RepeatEveryUnit.HasValue)
                        throw new InvalidOperationException("Interval schedules must define RepeatEveryUnit.");
                    break;

                case ScheduleType.Daily:
                    schedule.RepeatEveryValue = null;
                    schedule.RepeatEveryUnit = null;
                    schedule.DayOfWeek = null;
                    schedule.DayOfMonth = null;
                    break;

                case ScheduleType.Weekly:
                    schedule.RepeatEveryValue = null;
                    schedule.RepeatEveryUnit = null;
                    schedule.DayOfMonth = null;

                    if (!schedule.DayOfWeek.HasValue)
                        throw new InvalidOperationException("Weekly schedules must define DayOfWeek.");
                    break;

                case ScheduleType.Monthly:
                    schedule.RepeatEveryValue = null;
                    schedule.RepeatEveryUnit = null;
                    schedule.DayOfWeek = null;

                    if (!schedule.DayOfMonth.HasValue)
                        throw new InvalidOperationException("Monthly schedules must define DayOfMonth.");
                    break;

                case ScheduleType.Once:
                default:
                    schedule.RepeatEveryValue = null;
                    schedule.RepeatEveryUnit = null;
                    schedule.DayOfWeek = null;
                    schedule.DayOfMonth = null;
                    break;
            }
        }

        private static DateTime? CalculateNextRunUtc(SourceTransferSchedule schedule, DateTime referenceUtc)
        {
            var type = schedule.ScheduleTypeEnum;

            return type switch
            {
                ScheduleType.Once => schedule.StartDateUtc > referenceUtc ? schedule.StartDateUtc : referenceUtc,
                ScheduleType.Interval => CalculateIntervalNextRunUtc(schedule, referenceUtc),
                ScheduleType.Daily => CalculateDailyNextRunUtc(schedule, referenceUtc),
                ScheduleType.Weekly => CalculateWeeklyNextRunUtc(schedule, referenceUtc),
                ScheduleType.Monthly => CalculateMonthlyNextRunUtc(schedule, referenceUtc),
                _ => null
            };
        }

        private static DateTime? CalculateIntervalNextRunUtc(SourceTransferSchedule schedule, DateTime referenceUtc)
        {
            if (!schedule.RepeatEveryValue.HasValue || schedule.RepeatEveryValue.Value <= 0)
                return null;

            var unit = schedule.RepeatEveryUnitEnum ?? RepeatEveryUnit.Minutes;

            return unit switch
            {
                RepeatEveryUnit.Minutes => referenceUtc.AddMinutes(schedule.RepeatEveryValue.Value),
                RepeatEveryUnit.Hours => referenceUtc.AddHours(schedule.RepeatEveryValue.Value),
                _ => referenceUtc.AddMinutes(schedule.RepeatEveryValue.Value)
            };
        }

        private static DateTime? CalculateDailyNextRunUtc(SourceTransferSchedule schedule, DateTime referenceUtc)
        {
            var timeOfDay = schedule.TimeOfDayUtc ?? TimeSpan.Zero;
            var candidate = referenceUtc.Date.Add(timeOfDay);

            return candidate > referenceUtc ? candidate : candidate.AddDays(1);
        }

        private static DateTime? CalculateWeeklyNextRunUtc(SourceTransferSchedule schedule, DateTime referenceUtc)
        {
            if (!schedule.DayOfWeek.HasValue)
                return null;

            var timeOfDay = schedule.TimeOfDayUtc ?? TimeSpan.Zero;
            var candidate = referenceUtc.Date.Add(timeOfDay);

            while ((int)candidate.DayOfWeek != schedule.DayOfWeek.Value || candidate <= referenceUtc)
                candidate = candidate.AddDays(1);

            return candidate;
        }

        private static DateTime? CalculateMonthlyNextRunUtc(SourceTransferSchedule schedule, DateTime referenceUtc)
        {
            if (!schedule.DayOfMonth.HasValue)
                return null;

            var timeOfDay = schedule.TimeOfDayUtc ?? TimeSpan.Zero;

            var year = referenceUtc.Year;
            var month = referenceUtc.Month;

            var day = Math.Min(schedule.DayOfMonth.Value, DateTime.DaysInMonth(year, month));
            var candidate = new DateTime(year, month, day).Add(timeOfDay);

            if (candidate <= referenceUtc)
            {
                var next = new DateTime(year, month, 1).AddMonths(1);
                day = Math.Min(schedule.DayOfMonth.Value, DateTime.DaysInMonth(next.Year, next.Month));
                candidate = new DateTime(next.Year, next.Month, day).Add(timeOfDay);
            }

            return candidate;
        }
    }
}
