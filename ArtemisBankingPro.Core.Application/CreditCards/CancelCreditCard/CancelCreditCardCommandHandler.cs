using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.CreditCards.CancelCreditCard
{
    public class CancelCreditCardCommandHandler : IRequestHandler<CancelCreditCardCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CancelCreditCardCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(CancelCreditCardCommand request, CancellationToken cancellationToken)
        {
            var tarjetaRepo = _unitOfWork.Repository<TarjetaCredito>();
            var tarjeta = await tarjetaRepo.GetByIdAsync(request.Id);
            if (tarjeta == null)
                throw new NotFoundException("Tarjeta de crédito", request.Id);

            if (tarjeta.Estado == EstadoTarjeta.Cancelada)
                throw new BusinessRuleException("La tarjeta ya se encuentra cancelada.");

            if (tarjeta.MontoAdeudado > 0)
                throw new BusinessRuleException("Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la deuda pendiente.");

            tarjeta.Estado = EstadoTarjeta.Cancelada;
            tarjetaRepo.Update(tarjeta);

            // Persiste los cambios definitivamente en la base de datos
            await _unitOfWork.SaveChangesAsync();
        }
    }
}