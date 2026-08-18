using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Users.GetUserById
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailDto>
    {
        private readonly IIdentityService _identityService;

        public GetUserByIdQueryHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<UserDetailDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var userDetail = await _identityService.GetUserDetailByIdAsync(request.Id);

            if (userDetail == null)
            {
                throw new NotFoundException("Usuario", request.Id);
            }
            return userDetail;
        }
    }
}