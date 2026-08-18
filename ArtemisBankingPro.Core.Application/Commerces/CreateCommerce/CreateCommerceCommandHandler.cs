using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Commerces.CreateCommerce
{
    public class CreateCommerceCommandHandler : IRequestHandler<CreateCommerceCommand, CommerceResponseDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CreateCommerceCommandHandler(IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
        {
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<CommerceResponseDto> Handle(CreateCommerceCommand request, CancellationToken cancellationToken)
        {
            var comercioRepo = _unitOfWork.Repository<Comercio>();
            var existentes = await comercioRepo.FindAsync(c => c.Rnc == request.Rnc || c.Email == request.Email);

            if (existentes.Any(c => c.Rnc == request.Rnc))
                throw new ConflictException("Ya existe un comercio registrado con este RNC.");

            if (existentes.Any(c => c.Email == request.Email))
                throw new ConflictException("Ya existe un comercio registrado con este correo electrónico.");

            var comercio = new Comercio
            {
                Name = request.Name,
                Description = request.Description,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Rnc = request.Rnc,
                IsActive = true,
                FechaCreacion = _dateTimeProvider.UtcNow,
                CreatedByUserId = request.PerformedByUserId
            };

            await comercioRepo.AddAsync(comercio);
            await _unitOfWork.SaveChangesAsync();

            return new CommerceResponseDto(
                comercio.Id,
                comercio.Name,
                comercio.Description,
                comercio.Email,
                comercio.PhoneNumber,
                comercio.Rnc,
                comercio.IsActive,
                comercio.FechaCreacion
            );
        }
    }
}