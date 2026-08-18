using ArtemisBankingPro.Core.Domain.Entities;
using AutoMapper;

namespace ArtemisBankingPro.Core.Application.Account.Login
{
    public class LoginProfile : Profile
    {
        public LoginProfile()
        {
            CreateMap<ApplicationUser, LoginResultDto>()
                .ForCtorParam("UserId", opt => opt.MapFrom(src => src.Id));
        }
    }
}