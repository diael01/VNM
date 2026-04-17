using AutoMapper;
using Infrastructure.DTOs;
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

        public SourceTransferScheduleService(ISourceTransferScheduleRepository scheduleRepository, IMapper mapper)
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
            var created = await _scheduleRepository.AddAsync(entity, cancellationToken);
            return _mapper.Map<SourceTransferScheduleDto>(created);
        }

        public async Task<SourceTransferScheduleDto> UpdateAsync(int id, SourceTransferScheduleDto dto, CancellationToken cancellationToken = default)
        {
            var entity = _mapper.Map<SourceTransferSchedule>(dto);
            entity.Id = id;
            var updated = await _scheduleRepository.UpdateAsync(entity, cancellationToken);
            return _mapper.Map<SourceTransferScheduleDto>(updated);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _scheduleRepository.DeleteAsync(id, cancellationToken);
        }
    }
}
