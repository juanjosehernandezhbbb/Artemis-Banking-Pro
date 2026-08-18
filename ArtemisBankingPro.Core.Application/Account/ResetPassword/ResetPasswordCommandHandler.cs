using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Account.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
    {
        private readonly IIdentityService _identityService;

        public ResetPasswordCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var succeeded = await _identityService.ResetPasswordAsync(request.UserId, request.Token, request.Password);

            if (!succeeded)
            {
                throw new InvalidTokenException("El enlace de restablecimiento no es válido, ha expirado, o las contraseñas no cumplen con los requisitos.");
            }
        }
    }
}