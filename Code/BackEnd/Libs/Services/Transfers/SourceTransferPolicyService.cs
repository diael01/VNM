using AutoMapper;
using Infrastructure.DTOs;
using Repositories.CRUD.Repositories;
using Repositories.Models;

namespace Services.Transfers
{
    public interface ISourceTransferPolicyService
    {
        Task<List<SourceTransferPolicyDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<SourceTransferPolicyDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<SourceTransferPolicyDto> CreateAsync(SourceTransferPolicyDto dto, CancellationToken cancellationToken = default);
        Task<SourceTransferPolicyDto> UpdateAsync(int id, SourceTransferPolicyDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<List<TransferRuleDto>> GetRulesAsync(int policyId, CancellationToken cancellationToken = default);
        Task<List<SourceTransferScheduleDto>> GetSchedulesAsync(int policyId, CancellationToken cancellationToken = default);
    }

    public sealed class SourceTransferPolicyService : ISourceTransferPolicyService
    {
        private readonly ISourceTransferPolicyRepository _policyRepository;
        private readonly IMapper _mapper;

        public SourceTransferPolicyService(ISourceTransferPolicyRepository policyRepository, IMapper mapper)
        {
            _policyRepository = policyRepository;
            _mapper = mapper;
        }

        public async Task<List<SourceTransferPolicyDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var policies = await _policyRepository.GetAllWithCountsAsync(cancellationToken);
            return _mapper.Map<List<SourceTransferPolicyDto>>(policies);
        }

        public async Task<SourceTransferPolicyDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var policy = await _policyRepository.GetByIdWithChildrenAsync(id, cancellationToken);
            return policy is null ? null : _mapper.Map<SourceTransferPolicyDto>(policy);
        }

        public async Task<SourceTransferPolicyDto> CreateAsync(SourceTransferPolicyDto dto, CancellationToken cancellationToken = default)
        {
            var entity = _mapper.Map<SourceTransferPolicy>(dto);
            entity.Id = 0;
            var created = await _policyRepository.AddAsync(entity, cancellationToken);
            return _mapper.Map<SourceTransferPolicyDto>(created);
        }

        public async Task<SourceTransferPolicyDto> UpdateAsync(int id, SourceTransferPolicyDto dto, CancellationToken cancellationToken = default)
        {
            var entity = _mapper.Map<SourceTransferPolicy>(dto);
            entity.Id = id;
            var updated = await _policyRepository.UpdateAsync(entity, cancellationToken);
            return _mapper.Map<SourceTransferPolicyDto>(updated);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _policyRepository.DeleteAsync(id, cancellationToken);
        }

        public async Task<List<TransferRuleDto>> GetRulesAsync(int policyId, CancellationToken cancellationToken = default)
        {
            var policy = await _policyRepository.GetByIdWithChildrenAsync(policyId, cancellationToken);
            if (policy is null) return new List<TransferRuleDto>();
            return _mapper.Map<List<TransferRuleDto>>(policy.DestinationTransferRules);
        }

        public async Task<List<SourceTransferScheduleDto>> GetSchedulesAsync(int policyId, CancellationToken cancellationToken = default)
        {
            var policy = await _policyRepository.GetByIdWithChildrenAsync(policyId, cancellationToken);
            if (policy is null) return new List<SourceTransferScheduleDto>();
            return _mapper.Map<List<SourceTransferScheduleDto>>(policy.SourceTransferSchedules);
        }
    }
}
