using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Commerces.UpdateCommerce
{
    public class UpdateCommerceCommandHandler : IRequestHandler<UpdateCommerceCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCommerceCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateCommerceCommand request, CancellationToken cancellationToken)
        {
            var comercioRepo = _unitOfWork.Repository<Comercio>();
            var comercio = await comercioRepo.GetByIdAsync(request.Id);
            if (comercio == null)
                throw new NotFoundException("Comercio", request.Id);

            var existentes = await comercioRepo.FindAsync(c => (c.Rnc == request.Rnc || c.Email == request.Email) && c.Id != request.Id);

            if (existentes.Any(c => c.Rnc == request.Rnc))
                throw new ConflictException("El RNC pertenece a otro comercio.");

            if (existentes.Any(c => c.Email == request.Email))
                throw new ConflictException("El correo electrónico pertenece a otro comercio.");

            comercio.Name = request.Name;
            comercio.Description = request.Description;
            comercio.Email = request.Email;
            comercio.PhoneNumber = request.PhoneNumber;
            comercio.Rnc = request.Rnc;

            comercioRepo.Update(comercio);
        }
    }
}