using ArtemisBankingPro.Core.Application.Account.Login;
using ArtemisBankingPro.Core.Domain.Entities;
using AutoMapper;

namespace ArtemisBankingPro.Core.Application.Mappings
{
    public class LoginProfile : Profile
    {
        public LoginProfile()
        {
            CreateMap<ApplicationUser, LoginResultDto>()
                .ForCtorParam("UserId", opt => opt.MapFrom(src => src.Id))
                .ForCtorParam("Role", opt => opt.MapFrom(_ => (string)null))
                .ForCtorParam("CommerceId", opt => opt.MapFrom(_ => (int?)null));
        }
    }
}