using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.CreditCards.UpdateCreditCardLimit
{
    public class UpdateCreditCardLimitCommandHandler : IRequestHandler<UpdateCreditCardLimitCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UpdateCreditCardLimitCommandHandler> _logger;

        public UpdateCreditCardLimitCommandHandler(
            IUnitOfWork unitOfWork,
            IIdentityService identityService,
            IEmailService emailService,
            ILogger<UpdateCreditCardLimitCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Handle(UpdateCreditCardLimitCommand request, CancellationToken cancellationToken)
        {
            var tarjetaRepo = _unitOfWork.Repository<TarjetaCredito>();
            var tarjeta = await tarjetaRepo.GetByIdAsync(request.Id);
            if (tarjeta == null)
                throw new NotFoundException("Tarjeta de crédito", request.Id);

            if (tarjeta.Estado != EstadoTarjeta.Activa)
                throw new BusinessRuleException("Solo se puede modificar el límite de una tarjeta activa.");

            if (request.CreditLimit < tarjeta.MontoAdeudado)
                throw new BusinessRuleException("El nuevo límite no puede ser menor que la deuda actual de la tarjeta.");

            tarjeta.LimiteCredito = request.CreditLimit;
            tarjetaRepo.Update(tarjeta);

            var client = await _identityService.GetUserDetailByIdAsync(tarjeta.ClienteId);
            if (client != null)
            {
                try
                {
                    await _emailService.SendCardLimitChangeEmailAsync(
                        client.Email, $"{client.FirstName} {client.LastName}", tarjeta.NumeroTarjeta[^4..], request.CreditLimit);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falló el envío del correo de cambio de límite de la tarjeta {CardId}", tarjeta.Id);
                }
            }
        }
    }
}