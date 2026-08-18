using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Commerces.GetCommerceById
{
    public class GetCommerceByIdQueryHandler : IRequestHandler<GetCommerceByIdQuery, CommerceDetailDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;

        public GetCommerceByIdQueryHandler(IUnitOfWork unitOfWork, IIdentityService identityService)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
        }

        public async Task<CommerceDetailDto> Handle(GetCommerceByIdQuery request, CancellationToken cancellationToken)
        {
            var comercio = await _unitOfWork.Repository<Comercio>().GetByIdAsync(request.Id);
            if (comercio == null)
                throw new NotFoundException("Comercio", request.Id);

            CommerceAssociatedUserDto? associatedUser = null;
            if (comercio.ApplicationUserId != null)
            {
                var user = await _identityService.GetUserDetailByIdAsync(comercio.ApplicationUserId);
                if (user != null)
                {
                    associatedUser = new CommerceAssociatedUserDto(
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.IsActive);
                }
            }

            return new CommerceDetailDto(
                comercio.Id,
                comercio.Name,
                comercio.Description,
                comercio.Email,
                comercio.PhoneNumber,
                comercio.Rnc,
                comercio.IsActive,
                comercio.FechaCreacion,
                associatedUser);
        }
    }
}