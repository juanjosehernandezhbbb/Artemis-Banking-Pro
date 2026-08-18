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

namespace ArtemisBankingPro.Core.Application.CreditCards.CreateCreditCard
{
    public class CreateCreditCardCommandHandler : IRequestHandler<CreateCreditCardCommand, CreditCardResponseDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;
        private readonly ICreditCardService _creditCardService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEmailService _emailService;
        private readonly ILogger<CreateCreditCardCommandHandler> _logger;

        public CreateCreditCardCommandHandler(
            IUnitOfWork unitOfWork,
            IIdentityService identityService,
            ICreditCardService creditCardService,
            IDateTimeProvider dateTimeProvider,
            IEmailService emailService,
            ILogger<CreateCreditCardCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
            _creditCardService = creditCardService;
            _dateTimeProvider = dateTimeProvider;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<CreditCardResponseDto> Handle(CreateCreditCardCommand request, CancellationToken cancellationToken)
        {
            var client = await _identityService.GetUserDetailByIdAsync(request.ClientId);
            if (client == null || !client.IsActive)
                throw new BusinessRuleException("El cliente no existe o se encuentra inactivo.");

            string cardNumber;
            try
            {
                cardNumber = await _creditCardService.GenerateUniqueCardNumberAsync();
            }
            catch (InvalidOperationException ex)
            {
                throw new ConflictException(ex.Message);
            }

            var cvc = new Random().Next(100, 1000).ToString();

            var tarjeta = new TarjetaCredito
            {
                NumeroTarjeta = cardNumber,
                CvcHash = _creditCardService.HashCvc(cvc),
                ClienteId = request.ClientId,
                LimiteCredito = request.CreditLimit,
                MontoAdeudado = 0m,
                FechaExpiracion = _dateTimeProvider.UtcNow.AddYears(3),
                Estado = EstadoTarjeta.Activa,
                FechaCreacion = _dateTimeProvider.UtcNow
            };

            await _unitOfWork.Repository<TarjetaCredito>().AddAsync(tarjeta);
            await _unitOfWork.SaveChangesAsync();

            var lastFourDigits = cardNumber[^4..];

            try
            {
                await _emailService.SendCardAssignmentEmailAsync(client.Email, $"{client.FirstName} {client.LastName}", lastFourDigits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tarjeta {CardId} creada pero falló el envío del correo de asignación", tarjeta.Id);
            }

            return new CreditCardResponseDto(
                tarjeta.Id.ToString(),
                new string('*', 12) + lastFourDigits,
                lastFourDigits,
                client.Id,
                $"{client.FirstName} {client.LastName}",
                tarjeta.LimiteCredito,
                tarjeta.CreditoDisponible,
                tarjeta.MontoAdeudado,
                tarjeta.FechaExpiracion.ToString("MM/yy"),
                tarjeta.Estado.ToString(),
                tarjeta.FechaCreacion
            );
        }
    }
}