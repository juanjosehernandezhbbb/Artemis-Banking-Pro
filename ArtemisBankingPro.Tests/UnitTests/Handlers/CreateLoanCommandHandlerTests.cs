using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Loans.CreateLoan;
using ArtemisBankingPro.Core.Application.Users.GetUserById;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Tests.Helpers;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ArtemisBankingPro.Tests.UnitTests.Handlers
{
    public class CreateLoanCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identity = new();
        private readonly Mock<IAccountNumberGenerator> _accountGen = new();
        private readonly FakeUnitOfWork _uow = new();
        private readonly FakeDateTimeProvider _date = new();
        private readonly Mock<ILoanCalculatorService> _calc = new();
        private readonly Mock<IEmailService> _email = new();

        private CreateLoanCommandHandler Handler()
            => new(_identity.Object, _uow, _accountGen.Object, _date, _calc.Object, _email.Object);

        private void SetupHappyBase(string clientId = "client-1")
        {
            _identity.Setup(x => x.GetUserDetailByIdAsync(clientId)).ReturnsAsync(TestData.ActiveClient(clientId));
            _accountGen.Setup(x => x.GenerateUniqueAccountNumberAsync()).ReturnsAsync("200000001");
            _calc.Setup(x => x.CalculateFrenchAmortization(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<int>()))
                .Returns((1000m, 12000m));
            _calc.Setup(x => x.GenerateAmortizationSchedule(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<int>()))
                .Returns(TestData.AmortizationCuotas(0, 12, 1000m));
            _identity.Setup(x => x.GetActiveClientCountAsync()).ReturnsAsync(1);
        }

        private void SeedPrincipal(decimal balance = 1000m)
        {
            _uow.Repo<CuentaAhorro>().Items.Add(TestData.PrincipalAccount("client-1", balance));
        }

        private CreateLoanCommand Command(bool confirm = false, decimal capital = 100000m)
            => new("client-1", capital, 12, 12m, confirm, "admin-1");

        [Fact]
        public async Task ClienteInactivo_LanzaBusinessRule()
        {
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.InactiveClient());

            await Assert.ThrowsAsync<BusinessRuleException>(() => Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task ClienteYaTienePrestamoActivo_LanzaBusinessRule()
        {
            SetupHappyBase();
            _uow.Repo<Prestamo>().Items.Add(new Prestamo
            {
                Id = 1,
                NumeroPrestamo = "200000000",
                ClienteId = "client-1",
                Estado = EstadoPrestamo.Activo
            });

            await Assert.ThrowsAsync<BusinessRuleException>(() => Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task SinCuentaPrincipalActiva_LanzaBusinessRule()
        {
            SetupHappyBase();

            await Assert.ThrowsAsync<BusinessRuleException>(() => Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task DeudaActualSuperaPromedio_SinConfirmacion_LanzaHighRisk()
        {
            SetupHappyBase();
            SeedPrincipal();
            _uow.Repo<TarjetaCredito>().Items.Add(new TarjetaCredito
            {
                Id = 1,
                NumeroTarjeta = "1111222233334444",
                ClienteId = "client-1",
                MontoAdeudado = 8000m,
                LimiteCredito = 20000m
            });
            _identity.Setup(x => x.GetActiveClientCountAsync()).ReturnsAsync(2);

            var ex = await Assert.ThrowsAsync<HighRiskClientException>(() => Handler().Handle(Command(), CancellationToken.None));

            Assert.Equal("CurrentHighRisk", ex.RiskType);
            Assert.Equal(8000m, ex.CurrentDebt);
        }

        [Fact]
        public async Task DeudaProyectadaSuperaPromedio_SinConfirmacion_LanzaHighRisk()
        {
            SetupHappyBase();
            SeedPrincipal();
            _identity.Setup(x => x.GetActiveClientCountAsync()).ReturnsAsync(1);

            var ex = await Assert.ThrowsAsync<HighRiskClientException>(() => Handler().Handle(Command(), CancellationToken.None));

            Assert.Equal("ProjectedHighRisk", ex.RiskType);
        }

        [Fact]
        public async Task ConfirmarAltoRiesgo_CreaElPrestamo()
        {
            SetupHappyBase();
            SeedPrincipal();
            _uow.Repo<TarjetaCredito>().Items.Add(new TarjetaCredito
            {
                Id = 1,
                NumeroTarjeta = "1111222233334444",
                ClienteId = "client-1",
                MontoAdeudado = 8000m,
                LimiteCredito = 20000m
            });
            _identity.Setup(x => x.GetActiveClientCountAsync()).ReturnsAsync(2);

            var result = await Handler().Handle(Command(confirm: true), CancellationToken.None);

            Assert.Equal("200000001", result.LoanNumber);
            Assert.Equal(EstadoPrestamo.Activo.ToString(), result.Status);
            Assert.Equal(100000m, result.CapitalAmount);
            Assert.Equal(12, result.TermInMonths);

            var loan = Assert.Single(_uow.Repo<Prestamo>().Items);
            Assert.Equal("admin-1", loan.AdministradorId);
            Assert.Equal(12, _uow.Repo<CuotaAmortizacion>().Items.Count);

            var tx = Assert.Single(_uow.Repo<Transaccion>().Items);
            Assert.Equal(TipoOperacion.DesembolsoPrestamo, tx.TipoOperacion);
            Assert.Equal(TipoTransaccion.Credito, tx.Tipo);
            Assert.Equal(100000m, tx.Monto);
            Assert.Equal("admin-1", tx.RealizadoPorUserId);

            var account = _uow.Repo<CuentaAhorro>().Items.Single();
            Assert.Equal(101000m, account.Balance);

            _email.Verify(x => x.SendLoanApprovalEmailAsync(
                "juan.perez@example.com", "Juan", 100000m, "200000001", 12, 1000m), Times.Once);
        }

        [Fact]
        public async Task ClienteSinRiesgo_CreaPrestamoSinConfirmacion()
        {
            SetupHappyBase();
            SeedPrincipal();
            _uow.Repo<TarjetaCredito>().Items.Add(new TarjetaCredito
            {
                Id = 1,
                NumeroTarjeta = "1111222233334444",
                ClienteId = "other-client",
                MontoAdeudado = 100000m,
                LimiteCredito = 200000m
            });
            _identity.Setup(x => x.GetActiveClientCountAsync()).ReturnsAsync(5);

            var result = await Handler().Handle(Command(), CancellationToken.None);

            Assert.Equal(EstadoPrestamo.Activo.ToString(), result.Status);
            Assert.Single(_uow.Repo<Prestamo>().Items);
        }
    }
}