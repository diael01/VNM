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
        private readonly ISourceTransferScheduleRepository _scheduleRepository;
        private readonly ITransferRuleRepository _ruleRepository;
        private readonly ITransferWorkflowRepository _workflowRepository;
        private readonly IMapper _mapper;

        public SourceTransferPolicyService(
            ISourceTransferPolicyRepository policyRepository,
            ISourceTransferScheduleRepository scheduleRepository,
            ITransferRuleRepository ruleRepository,
            ITransferWorkflowRepository workflowRepository,
            IMapper mapper)
        {
            _policyRepository = policyRepository;
            _scheduleRepository = scheduleRepository;
            _ruleRepository = ruleRepository;
            _workflowRepository = workflowRepository;
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
            var existing = await _policyRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"SourceTransferPolicy {id} was not found.");

            // Update only mutable business fields and preserve audit fields/relationships.
            existing.SourceAddressId = dto.SourceAddressId;
            existing.DistributionMode = dto.DistributionMode;
            existing.IsEnabled = dto.IsEnabled;

            var updated = await _policyRepository.UpdateAsync(existing, cancellationToken);
            return _mapper.Map<SourceTransferPolicyDto>(updated);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _policyRepository.GetByIdAsync(id, cancellationToken);
            if (existing is null) return false;

            // Delete destination rules under this policy and clear workflow references first.
            var rules = await _ruleRepository.FindAsync(r => r.SourceTransferPolicyId == id, cancellationToken);
            foreach (var rule in rules)
            {
                var linkedWorkflows = await _workflowRepository.FindAsync(
                    w => w.DestinationTransferRuleId == rule.Id,
                    cancellationToken);

                foreach (var workflow in linkedWorkflows)
                {
                    workflow.DestinationTransferRuleId = null;
                    await _workflowRepository.UpdateAsync(workflow, cancellationToken);
                }

                await _ruleRepository.DeleteAsync(rule.Id, cancellationToken);
            }

            // Delete schedules under this policy.
            var schedules = await _scheduleRepository.FindAsync(s => s.SourceTransferPolicyId == id, cancellationToken);
            foreach (var schedule in schedules)
            {
                await _scheduleRepository.DeleteAsync(schedule.Id, cancellationToken);
            }

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
