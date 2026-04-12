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
            CreateMap<TransferRule, TransferRuleDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SourceAddressId, opt => opt.MapFrom(src => src.SourceAddressId))
                .ForMember(dest => dest.DestinationAddressId, opt => opt.MapFrom(src => src.DestinationAddressId))
                .ForMember(dest => dest.IsEnabled, opt => opt.MapFrom(src => src.IsEnabled))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.DistributionMode, opt => opt.MapFrom(src => src.DistributionMode))
                .ForMember(dest => dest.MaxDailyKwh, opt => opt.MapFrom(src => src.MaxDailyKwh))
                .ForMember(dest => dest.WeightPercent, opt => opt.MapFrom(src => src.WeightPercent));

            // DTO -> Entity (for create/update)
            CreateMap<TransferRuleDto, TransferRule>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SourceAddressId, opt => opt.MapFrom(src => src.SourceAddressId))
                .ForMember(dest => dest.DestinationAddressId, opt => opt.MapFrom(src => src.DestinationAddressId))
                .ForMember(dest => dest.IsEnabled, opt => opt.MapFrom(src => src.IsEnabled))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.DistributionMode, opt => opt.MapFrom(src => src.DistributionMode))
                .ForMember(dest => dest.MaxDailyKwh, opt => opt.MapFrom(src => src.MaxDailyKwh))
                .ForMember(dest => dest.WeightPercent, opt => opt.MapFrom(src => src.WeightPercent));
        }
    }
}
