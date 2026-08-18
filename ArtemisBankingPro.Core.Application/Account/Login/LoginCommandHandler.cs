using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Common.Exceptions; 
using AutoMapper;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Account.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto>
    {
        private readonly IIdentityService _identityService;
        private readonly IMapper _mapper;

        public LoginCommandHandler(IIdentityService identityService, IMapper mapper)
        {
            _identityService = identityService;
            _mapper = mapper;
        }

        public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var (succeeded, user) = await _identityService.ValidateCredentialsAsync(request.UserName, request.Password);

            if (!succeeded || user == null)
            {
                throw new UnauthorizedAccessException("Los datos de acceso son inválidos.");
            }

            if (!user.Activo)
            {
                throw new UnauthorizedAccessException("Su cuenta se encuentra inactiva. Debe activar su cuenta...");
            }

            var userRoles = await _identityService.GetRolesAsync(user);
            var matchedRole = userRoles.FirstOrDefault(r => request.AllowedRoles.Contains(r));

            if (matchedRole == null)
            {
                throw new ForbiddenAccessException("Este usuario no tiene permisos para acceder a esta aplicación.");
            }

            var result = _mapper.Map<LoginResultDto>(user);

            int? commerceId = null;
            if (matchedRole == DomainConstants.RolComercio)
            {
                commerceId = await _identityService.GetCommerceIdByUserIdAsync(user.Id);
            }

            return result with { Role = matchedRole, CommerceId = commerceId };
        }
    }
}