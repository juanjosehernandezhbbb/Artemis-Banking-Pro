using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Users.ChangeUserStatus
{
    public class ChangeUserStatusCommandHandler : IRequestHandler<ChangeUserStatusCommand>
    {
        private readonly IIdentityService _identityService;

        public ChangeUserStatusCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task Handle(ChangeUserStatusCommand request, CancellationToken cancellationToken)
        {
            var userDetail = await _identityService.GetUserDetailByIdAsync(request.Id);

            if (userDetail == null)
            {
                throw new NotFoundException("Usuario", request.Id);
            }

            if (request.Id == request.PerformedByUserId)
            {
                throw new ForbiddenAccessException("No puede modificar su propio estado.");
            }

            await _identityService.SetUserActiveStatusAsync(request.Id, request.Status!.Value);
        }
    }
}