using AutoMapper;
using Infrastructure.DTOs;
using Repositories.Models;

namespace Services.Profiles
{
    public class TransferRuleProfile : Profile
    {
        public TransferRuleProfile()
        {
            // Entity -> DTO (for API responses)
            CreateMap<DestinationTransferRule, TransferRuleDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SourceTransferPolicyId, opt => opt.MapFrom(src => src.SourceTransferPolicyId))
                .ForMember(dest => dest.DestinationAddressId, opt => opt.MapFrom(src => src.DestinationAddressId))
                .ForMember(dest => dest.IsEnabled, opt => opt.MapFrom(src => src.IsEnabled))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.DistributionMode, opt => opt.MapFrom(src => src.DistributionMode))
                .ForMember(dest => dest.MaxDailyKwh, opt => opt.MapFrom(src => src.MaxDailyKwh))
                .ForMember(dest => dest.WeightPercent, opt => opt.MapFrom(src => src.WeightPercent))
                .ForMember(dest => dest.UpdatedAtUtc, opt => opt.MapFrom(src => src.UpdatedAtUtc));

            // DTO -> Entity (for create/update)
            CreateMap<TransferRuleDto, DestinationTransferRule>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SourceTransferPolicyId, opt => opt.MapFrom(src => src.SourceTransferPolicyId))
                .ForMember(dest => dest.DestinationAddressId, opt => opt.MapFrom(src => src.DestinationAddressId))
                .ForMember(dest => dest.IsEnabled, opt => opt.MapFrom(src => src.IsEnabled))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.DistributionMode, opt => opt.MapFrom(src => src.DistributionMode))
                .ForMember(dest => dest.MaxDailyKwh, opt => opt.MapFrom(src => src.MaxDailyKwh))
                .ForMember(dest => dest.WeightPercent, opt => opt.MapFrom(src => src.WeightPercent));
        }
    }
}
