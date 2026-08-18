using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ArtemisBankingPro.Core.Application.Users.CreateUser
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, string>
    {
        private readonly IIdentityService _identityService;
        private readonly IAccountNumberGenerator _accountNumberGenerator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IIdentityService identityService,
            IAccountNumberGenerator accountNumberGenerator,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IDateTimeProvider dateTimeProvider,
            ILogger<CreateUserCommandHandler> logger)
        {
            _identityService = identityService;
            _accountNumberGenerator = accountNumberGenerator;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }

        public async Task<string> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            if (await _identityService.CheckCedulaExistsAsync(request.Identification))
                throw new ConflictException("Ya existe un usuario registrado con esta cédula.");

            if (await _identityService.CheckEmailExistsAsync(request.Email))
                throw new ConflictException("Ya existe un usuario registrado con este correo electrónico.");

            if (await _identityService.CheckUserNameExistsAsync(request.UserName))
                throw new ConflictException("Ya existe un usuario registrado con este nombre de usuario.");

            var user = new ApplicationUser
            {
                Nombre = request.FirstName,
                Apellido = request.LastName,
                Cedula = request.Identification,
                Email = request.Email,
                UserName = request.UserName,
                Activo = false,
                FechaCreacion = _dateTimeProvider.UtcNow
            };

            var (succeeded, errors) = await _identityService.CreateUserAsync(user, request.Password, request.Role);
            if (!succeeded)
                throw new BusinessRuleException($"Error al crear usuario: {string.Join(", ", errors)}");

            if (request.Role == DomainConstants.RolCliente)
            {
                var accountNumber = await _accountNumberGenerator.GenerateUniqueAccountNumberAsync();

                var account = new CuentaAhorro
                {
                    NumeroCuenta = accountNumber,
                    ClienteId = user.Id,
                    Balance = request.InitialAmount,
                    TipoCuenta = TipoCuenta.Principal,
                    Estado = EstadoCuenta.Activa,
                    FechaCreacion = _dateTimeProvider.UtcNow
                };

                await _unitOfWork.Repository<CuentaAhorro>().AddAsync(account);

                if (request.InitialAmount > 0)
                {
                    var transaction = new Transaccion
                    {
                        CuentaAhorro = account,
                        TipoOperacion = TipoOperacion.MontoInicial,
                        Tipo = TipoTransaccion.Credito,
                        Monto = request.InitialAmount,
                        Origen = "APERTURA DE CUENTA",
                        Destino = account.NumeroCuenta,
                        Estado = EstadoTransaccion.Aprobada,
                        Fecha = _dateTimeProvider.UtcNow,
                        RealizadoPorUserId = null
                    };
                    await _unitOfWork.Repository<Transaccion>().AddAsync(transaction);
                }

                await _unitOfWork.SaveChangesAsync();
            }

            var nativeToken = await _identityService.GenerateEmailConfirmationTokenAsync(user);
            var combinedToken = $"{user.Id}:{nativeToken}";
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(combinedToken));

            try
            {
                await _emailService.SendActivationEmailAsync(user.Email!, user.Nombre, encodedToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Usuario {UserId} creado pero falló el envío del correo de activación", user.Id);
            }

            return user.Id;
        }
    }
}