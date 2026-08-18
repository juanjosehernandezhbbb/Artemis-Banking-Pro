using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Account.GetResetToken
{
    public class GetResetTokenCommandHandler : IRequestHandler<GetResetTokenCommand>
    {
        private readonly IIdentityService _identityService;
        private readonly IEmailService _emailService;

        public GetResetTokenCommandHandler(IIdentityService identityService, IEmailService emailService)
        {
            _identityService = identityService;
            _emailService = emailService;
        }

        public async Task Handle(GetResetTokenCommand request, CancellationToken cancellationToken)
        {
            var user = await _identityService.FindByUserNameAsync(request.UserName);
            if (user == null)
                throw new BusinessRuleException("El usuario no existe o no tiene correo registrado.");

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new BusinessRuleException("El usuario no tiene un correo electrónico registrado válido.");

            var roles = await _identityService.GetRolesAsync(user);
            if (!roles.Any(r => request.AllowedRoles.Contains(r)))
                throw new BusinessRuleException("Este usuario no puede solicitar restablecimiento por este canal.");

            var token = await _identityService.GeneratePasswordResetTokenAsync(user);
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Nombre, token);
        }
    }
}