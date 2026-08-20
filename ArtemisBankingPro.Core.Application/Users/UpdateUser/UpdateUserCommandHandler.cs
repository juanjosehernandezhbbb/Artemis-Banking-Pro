using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Domain.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Users.UpdateUser
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
    {
        private readonly IIdentityService _identityService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UpdateUserCommandHandler(IIdentityService identityService, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
        {
            _identityService = identityService;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var userDetail = await _identityService.GetUserDetailByIdAsync(request.Id);
            if (userDetail == null)
            {
                throw new NotFoundException("Usuario", request.Id);
            }

            if (await _identityService.CheckCedulaExistsAsync(request.Identification, request.Id))
                throw new ConflictException("Ya existe otro usuario registrado con esta cédula.");

            if (await _identityService.CheckEmailExistsAsync(request.Email, request.Id))
                throw new ConflictException("Ya existe otro usuario registrado con este correo electrónico.");

            if (await _identityService.CheckUserNameExistsAsync(request.UserName, request.Id))
                throw new ConflictException("Ya existe otro usuario registrado con este nombre de usuario.");

            var success = await _identityService.UpdateUserAdminAsync(
                request.Id, request.FirstName, request.LastName,
                request.Identification, request.Email, request.UserName, request.Password);

            if (!success)
            {
                throw new BusinessRuleException("Ocurrió un error actualizando el usuario en el sistema de identidad.");
            }

            if (request.AdditionalAmount > 0 &&
               (userDetail.Role == DomainConstants.RolCliente || userDetail.Role == DomainConstants.RolComercio))
            {
                var cuentaRepository = _unitOfWork.Repository<CuentaAhorro>();
                var transaccionRepository = _unitOfWork.Repository<Transaccion>();

                var cuentas = await cuentaRepository.FindAsync(c => c.ClienteId == request.Id && c.TipoCuenta == TipoCuenta.Principal);
                var cuentaPrincipal = cuentas.FirstOrDefault();

                // Si el cliente no tiene cuenta principal, se la creamos automáticamente para poder abonarle
                if (cuentaPrincipal == null)
                {
                    cuentaPrincipal = new CuentaAhorro
                    {
                        ClienteId = request.Id,
                        NumeroCuenta = new Random().Next(100000000, 999999999).ToString(), // Generador de 9 dígitos temporal
                        Balance = 0m,
                        TipoCuenta = TipoCuenta.Principal,
                        Estado = EstadoCuenta.Activa,
                        FechaCreacion = _dateTimeProvider.UtcNow
                    };
                    await cuentaRepository.AddAsync(cuentaPrincipal);
                }

                cuentaPrincipal.Balance += request.AdditionalAmount;
                cuentaRepository.Update(cuentaPrincipal);

                var transaccion = new Transaccion
                {
                    CuentaAhorroId = cuentaPrincipal.Id,
                    Tipo = TipoTransaccion.Credito,
                    TipoOperacion = TipoOperacion.MontoAdicionalAdmin,
                    Monto = request.AdditionalAmount,
                    Origen = "AJUSTE ADMINISTRATIVO",
                    Destino = cuentaPrincipal.NumeroCuenta,
                    Estado = EstadoTransaccion.Aprobada,
                    Fecha = _dateTimeProvider.UtcNow,
                    RealizadoPorUserId = request.PerformedByUserId
                };

                await transaccionRepository.AddAsync(transaccion);
            }
        }
    }
}