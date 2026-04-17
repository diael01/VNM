using AutoMapper;
using Infrastructure.DTOs;
using Repositories.Models;

namespace Services.Profiles
{
    public class SourceTransferPolicyProfile : Profile
    {
        public SourceTransferPolicyProfile()
        {
            CreateMap<SourceTransferPolicy, SourceTransferPolicyDto>()
                .ForMember(dest => dest.DestinationRulesCount, opt => opt.MapFrom(src => src.DestinationTransferRules.Count))
                .ForMember(dest => dest.SchedulesCount, opt => opt.MapFrom(src => src.SourceTransferSchedules.Count));

            CreateMap<SourceTransferPolicyDto, SourceTransferPolicy>()
                .ForMember(dest => dest.DestinationTransferRules, opt => opt.Ignore())
                .ForMember(dest => dest.SourceTransferSchedules, opt => opt.Ignore())
                .ForMember(dest => dest.SourceAddress, opt => opt.Ignore());

            CreateMap<SourceTransferSchedule, SourceTransferScheduleDto>();

            CreateMap<SourceTransferScheduleDto, SourceTransferSchedule>()
                .ForMember(dest => dest.SourceTransferPolicy, opt => opt.Ignore());
        }
    }
}
