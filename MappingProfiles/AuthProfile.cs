using AutoMapper;
using UserAuthentication_ASPNET.Models.Dtos;
using UserAuthentication_ASPNET.Models.Entities;

namespace UserAuthentication_ASPNET.MappingProfiles
{
    public class AuthProfile : Profile
    {
        public AuthProfile()
        {
            CreateMap<AuthRegisterDto, User>()
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.Password));
        }
    }
}