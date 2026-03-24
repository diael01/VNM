using AutoMapper;
using Infrastructure.DTOs;
using Repositories.Models;

namespace Services.Profiles
{
    public class AddressProfile : Profile
    {
        public AddressProfile()
        {
            // Entity -> DTO (for API responses)
            CreateMap<Address, AddressDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));

            // DTO -> Entity (for create/update)
            CreateMap<AddressDto, Address>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));
        }
    }
}
