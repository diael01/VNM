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
        private readonly IMapper _mapper;

        public TransferRuleService(ITransferRuleRepository transferRuleRepository, IMapper mapper)
        {
            _transferRuleRepository = transferRuleRepository;
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
            var rule = _mapper.Map<DestinationTransferRule>(transferRuleDto);
            rule.Id = 0;
            var created = await _transferRuleRepository.AddAsync(rule);
            return _mapper.Map<TransferRuleDto>(created);
        }

        public async Task<TransferRuleDto> UpdateAsync(int id, TransferRuleDto transferRuleDto)
        {
            var rule = _mapper.Map<DestinationTransferRule>(transferRuleDto);
            rule.Id = id;
            var updated = await _transferRuleRepository.UpdateAsync(rule);
            return _mapper.Map<TransferRuleDto>(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _transferRuleRepository.DeleteAsync(id);
        }
    }
}
