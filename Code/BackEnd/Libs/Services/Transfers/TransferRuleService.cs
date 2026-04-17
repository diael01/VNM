using AutoMapper;
using Infrastructure.DTOs;
using Repositories.CRUD.Repositories;
using Repositories.Models;

namespace Services.Transfers
{
    public interface ITransferRuleService
    {
        Task<List<TransferRuleDto>> GetAllAsync();
        Task<TransferRuleDto?> GetByIdAsync(int id);
        Task<TransferRuleDto> CreateAsync(TransferRuleDto transferRuleDto);
        Task<TransferRuleDto> UpdateAsync(int id, TransferRuleDto transferRuleDto);
        Task<bool> DeleteAsync(int id);
    }

    public sealed class TransferRuleService : ITransferRuleService
    {
        private readonly ITransferRuleRepository _transferRuleRepository;
        private readonly ISourceTransferPolicyRepository _sourceTransferPolicyRepository;
        private readonly ITransferWorkflowRepository _transferWorkflowRepository;
        private readonly IMapper _mapper;

        public TransferRuleService(
            ITransferRuleRepository transferRuleRepository,
            ISourceTransferPolicyRepository sourceTransferPolicyRepository,
            ITransferWorkflowRepository transferWorkflowRepository,
            IMapper mapper)
        {
            _transferRuleRepository = transferRuleRepository;
            _sourceTransferPolicyRepository = sourceTransferPolicyRepository;
            _transferWorkflowRepository = transferWorkflowRepository;
            _mapper = mapper;
        }

        public async Task<List<TransferRuleDto>> GetAllAsync()
        {
            var rules = await _transferRuleRepository.GetAllAsync();
            return _mapper.Map<List<TransferRuleDto>>(rules);
        }

        public async Task<TransferRuleDto?> GetByIdAsync(int id)
        {
            var rule = await _transferRuleRepository.GetByIdAsync(id);
            return rule == null ? null : _mapper.Map<TransferRuleDto>(rule);
        }

        public async Task<TransferRuleDto> CreateAsync(TransferRuleDto transferRuleDto)
        {
            await ValidateSourceAndDestinationAsync(
                transferRuleDto.SourceTransferPolicyId,
                transferRuleDto.DestinationAddressId);

            var rule = _mapper.Map<DestinationTransferRule>(transferRuleDto);
            rule.Id = 0;
            var created = await _transferRuleRepository.AddAsync(rule);
            return _mapper.Map<TransferRuleDto>(created);
        }

        public async Task<TransferRuleDto> UpdateAsync(int id, TransferRuleDto transferRuleDto)
        {
            var existing = await _transferRuleRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"DestinationTransferRule {id} was not found.");

            await ValidateSourceAndDestinationAsync(
                transferRuleDto.SourceTransferPolicyId,
                transferRuleDto.DestinationAddressId);

            existing.SourceTransferPolicyId = transferRuleDto.SourceTransferPolicyId;
            existing.DestinationAddressId = transferRuleDto.DestinationAddressId;
            existing.IsEnabled = transferRuleDto.IsEnabled;
            existing.Priority = transferRuleDto.Priority;
            existing.MaxDailyKwh = transferRuleDto.MaxDailyKwh;
            existing.WeightPercent = transferRuleDto.WeightPercent;

            var updated = await _transferRuleRepository.UpdateAsync(existing);
            return _mapper.Map<TransferRuleDto>(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var linkedWorkflows = await _transferWorkflowRepository.FindAsync(w => w.DestinationTransferRuleId == id);
            foreach (var workflow in linkedWorkflows)
            {
                workflow.DestinationTransferRuleId = null;
                await _transferWorkflowRepository.UpdateAsync(workflow);
            }

            return await _transferRuleRepository.DeleteAsync(id);
        }

        private async Task ValidateSourceAndDestinationAsync(int sourceTransferPolicyId, int destinationAddressId)
        {
            var policy = await _sourceTransferPolicyRepository.GetByIdAsync(sourceTransferPolicyId)
                ?? throw new KeyNotFoundException($"SourceTransferPolicy {sourceTransferPolicyId} was not found.");

            if (policy.SourceAddressId == destinationAddressId)
            {
                throw new InvalidOperationException(
                    "Source and destination cannot be the same address.");
            }
        }
    }
}
