using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Users.CreateUser;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ArtemisBankingPro.Tests.UnitTests.Handlers
{
    public class CreateUserCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identity = new();
        private readonly Mock<IAccountNumberGenerator> _accountGen = new();
        private readonly FakeUnitOfWork _uow = new();
        private readonly Mock<IEmailService> _email = new();
        private readonly FakeDateTimeProvider _date = new();

        private CreateUserCommandHandler Handler()
            => new(_identity.Object, _accountGen.Object, _uow, _email.Object, _date, NullLogger<CreateUserCommandHandler>.Instance);

        private static CreateUserCommand Command(string role = DomainConstants.RolCliente, decimal initial = 500m)
            => new("Juan", "Perez", "00112345678", "juan@test.com", "juanp", "Passw0rd!", "Passw0rd!", role, initial);

        private void SetupCreateSucceeded()
        {
            _identity
                .Setup(x => x.CreateUserAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<ApplicationUser, string, string>((user, _, _) => user.Id = "user-1")
                .ReturnsAsync((true, new List<string>()));
            _accountGen.Setup(x => x.GenerateUniqueAccountNumberAsync()).ReturnsAsync("100000002");
            _identity.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("native-token");
        }

        [Fact]
        public async Task CedulaDuplicada_LanzaConflict()
        {
            _identity.Setup(x => x.CheckCedulaExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() => Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task EmailDuplicado_LanzaConflict()
        {
            _identity.Setup(x => x.CheckEmailExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() => Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task UserNameDuplicado_LanzaConflict()
        {
            _identity.Setup(x => x.CheckUserNameExistsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() => Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task CreacionDeUsuarioFalla_LanzaBusinessRule()
        {
            _identity
                .Setup(x => x.CreateUserAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((false, new List<string> { "error" }));

            await Assert.ThrowsAsync<BusinessRuleException>(() => Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task Cliente_CreaCuentaPrincipalConSaldoYTransaccion()
        {
            SetupCreateSucceeded();

            var id = await Handler().Handle(Command(initial: 500m), CancellationToken.None);

            Assert.Equal("user-1", id);
            var account = Assert.Single(_uow.Repo<CuentaAhorro>().Items);
            Assert.Equal(TipoCuenta.Principal, account.TipoCuenta);
            Assert.Equal(EstadoCuenta.Activa, account.Estado);
            Assert.Equal(500m, account.Balance);
            Assert.Equal("user-1", account.ClienteId);

            var tx = Assert.Single(_uow.Repo<Transaccion>().Items);
            Assert.Equal(TipoOperacion.MontoInicial, tx.TipoOperacion);
            Assert.Equal(TipoTransaccion.Credito, tx.Tipo);
            Assert.Equal(EstadoTransaccion.Aprobada, tx.Estado);
            Assert.Equal(500m, tx.Monto);
        }

        [Fact]
        public async Task ClienteConMontoCero_NoCreaTransaccion()
        {
            SetupCreateSucceeded();

            await Handler().Handle(Command(initial: 0m), CancellationToken.None);

            Assert.Single(_uow.Repo<CuentaAhorro>().Items);
            Assert.Empty(_uow.Repo<Transaccion>().Items);
        }

        [Fact]
        public async Task RolCajero_NoCreaCuentaDeAhorro()
        {
            SetupCreateSucceeded();

            await Handler().Handle(Command(role: DomainConstants.RolCajero), CancellationToken.None);

            Assert.Empty(_uow.Repo<CuentaAhorro>().Items);
            Assert.Empty(_uow.Repo<Transaccion>().Items);
        }

        [Fact]
        public async Task UsuarioNaceInactivo_YSeEnviaEmailDeActivacion()
        {
            ApplicationUser? captured = null;
            _identity
                .Setup(x => x.CreateUserAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<ApplicationUser, string, string>((user, _, _) => { user.Id = "user-1"; captured = user; })
                .ReturnsAsync((true, new List<string>()));
            _accountGen.Setup(x => x.GenerateUniqueAccountNumberAsync()).ReturnsAsync("100000002");
            _identity.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("native-token");

            await Handler().Handle(Command(), CancellationToken.None);

            Assert.False(captured!.Activo);
            _email.Verify(x => x.SendActivationEmailAsync("juan@test.com", "Juan", It.IsAny<string>()), Times.Once);
        }
    }
}