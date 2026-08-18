using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Account.ConfirmAccount
{
    public class ConfirmAccountCommandHandler : IRequestHandler<ConfirmAccountCommand>
    {
        private readonly IIdentityService _identityService;

        public ConfirmAccountCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task Handle(ConfirmAccountCommand request, CancellationToken cancellationToken)
        {
            var succeeded = await _identityService.ConfirmAccountAsync(request.Token);

            if (!succeeded)
            {
                throw new InvalidTokenException("El token proporcionado es inválido, ha expirado, ya fue utilizado o no está asociado a un usuario válido.");
            }
        }
    }
}