using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Commerces.ChangeCommerceStatus
{
    public class ChangeCommerceStatusCommandHandler : IRequestHandler<ChangeCommerceStatusCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;

        public ChangeCommerceStatusCommandHandler(IUnitOfWork unitOfWork, IIdentityService identityService)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
        }

        public async Task Handle(ChangeCommerceStatusCommand request, CancellationToken cancellationToken)
        {
            var comercioRepo = _unitOfWork.Repository<Comercio>();
            var comercio = await comercioRepo.GetByIdAsync(request.Id);
            if (comercio == null)
                throw new NotFoundException("Comercio", request.Id);

            comercio.IsActive = request.Status;
            comercioRepo.Update(comercio);

            if (!request.Status && comercio.ApplicationUserId != null)
            {
                await _identityService.SetUserActiveStatusAsync(comercio.ApplicationUserId, false);
            }
        }
    }
}