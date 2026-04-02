using AutoMapper;
using Repositories.Models;
using Infrastructure.DTOs;

namespace Services.Assets.Profiles
{
    public class InverterProfile : Profile
    {
        public InverterProfile()
        {
            CreateMap<InverterInfo, InverterInfoDto>().ReverseMap();
        }
    }
}
