using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.SavingsAccounts.CancelSavingsAccount;
using ArtemisBankingPro.Core.Application.SavingsAccounts.CreateSavingsAccount;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Tests.Helpers;
using Moq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ArtemisBankingPro.Tests.UnitTests.Handlers
{
    public class CreateSavingsAccountCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly Mock<IIdentityService> _identity = new();
        private readonly Mock<IAccountNumberGenerator> _accountGen = new();
        private readonly FakeDateTimeProvider _date = new();

        private CreateSavingsAccountCommandHandler Handler()
            => new(_uow, _identity.Object, _accountGen.Object, _date);

        [Fact]
        public async Task ClienteInactivo_LanzaBusinessRule()
        {
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.InactiveClient());

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                Handler().Handle(new CreateSavingsAccountCommand("client-1", 500m), CancellationToken.None));
        }

        [Fact]
        public async Task SinCuentaPrincipalActiva_LanzaBusinessRule()
        {
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.ActiveClient());
            _uow.Repo<CuentaAhorro>().Items.Add(new CuentaAhorro
            {
                Id = 1,
                NumeroCuenta = "100000001",
                ClienteId = "client-1",
                Balance = 0m,
                TipoCuenta = TipoCuenta.Principal,
                Estado = EstadoCuenta.Cancelada
            });

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                Handler().Handle(new CreateSavingsAccountCommand("client-1", 500m), CancellationToken.None));
        }

        [Fact]
        public async Task ClienteActivo_CreaCuentaSecundariaConTransaccion()
        {
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.ActiveClient());
            _uow.Repo<CuentaAhorro>().Items.Add(TestData.PrincipalAccount());
            _accountGen.Setup(x => x.GenerateUniqueAccountNumberAsync()).ReturnsAsync("100000002");

            var result = await Handler().Handle(new CreateSavingsAccountCommand("client-1", 500m, "cajero-1"), CancellationToken.None);

            var account = Assert.Single(_uow.Repo<CuentaAhorro>().Items.Where(a => a.TipoCuenta == TipoCuenta.Secundaria));
            Assert.Equal("100000002", account.NumeroCuenta);
            Assert.Equal(500m, account.Balance);
            Assert.Equal(TipoCuenta.Secundaria, account.TipoCuenta);
            Assert.Equal(EstadoCuenta.Activa, account.Estado);

            var tx = Assert.Single(_uow.Repo<Transaccion>().Items);
            Assert.Equal(TipoOperacion.MontoInicial, tx.TipoOperacion);
            Assert.Equal(TipoTransaccion.Credito, tx.Tipo);
            Assert.Equal(500m, tx.Monto);
            Assert.Equal("cajero-1", tx.RealizadoPorUserId);

            Assert.Equal("100000002", result.AccountNumber);
            Assert.Equal("Secundaria", result.Type);
        }

        [Fact]
        public async Task BalanceCero_NoCreaTransaccion()
        {
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.ActiveClient());
            _uow.Repo<CuentaAhorro>().Items.Add(TestData.PrincipalAccount());
            _accountGen.Setup(x => x.GenerateUniqueAccountNumberAsync()).ReturnsAsync("100000002");

            await Handler().Handle(new CreateSavingsAccountCommand("client-1", 0m, "cajero-1"), CancellationToken.None);

            Assert.Empty(_uow.Repo<Transaccion>().Items);
        }
    }

    public class CancelSavingsAccountCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly FakeDateTimeProvider _date = new();
        private readonly CancelSavingsAccountCommandHandler _handler;

        public CancelSavingsAccountCommandHandlerTests()
        {
            _handler = new CancelSavingsAccountCommandHandler(_uow, _date);
        }

        private void SeedSecondaryAccount(decimal balance = 500m, EstadoCuenta estado = EstadoCuenta.Activa)
        {
            _uow.Repo<CuentaAhorro>().Items.Add(new CuentaAhorro
            {
                Id = 2,
                NumeroCuenta = "100000002",
                ClienteId = "client-1",
                Balance = balance,
                TipoCuenta = TipoCuenta.Secundaria,
                Estado = estado
            });
            _uow.Repo<CuentaAhorro>().Items.Add(TestData.PrincipalAccount());
        }

        [Fact]
        public async Task CuentaNoExiste_LanzaNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(new CancelSavingsAccountCommand("999999999", "cajero-1"), CancellationToken.None));
        }

        [Fact]
        public async Task CuentaPrincipal_LanzaBusinessRule()
        {
            _uow.Repo<CuentaAhorro>().Items.Add(TestData.PrincipalAccount());

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _handler.Handle(new CancelSavingsAccountCommand("100000001", "cajero-1"), CancellationToken.None));
        }

        [Fact]
        public async Task CuentaYaCancelada_LanzaBusinessRule()
        {
            SeedSecondaryAccount(estado: EstadoCuenta.Cancelada);

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _handler.Handle(new CancelSavingsAccountCommand("100000002", "cajero-1"), CancellationToken.None));
        }

        [Fact]
        public async Task SinCuentaPrincipalActiva_LanzaBusinessRule()
        {
            _uow.Repo<CuentaAhorro>().Items.Add(new CuentaAhorro
            {
                Id = 2,
                NumeroCuenta = "100000002",
                ClienteId = "client-1",
                Balance = 500m,
                TipoCuenta = TipoCuenta.Secundaria,
                Estado = EstadoCuenta.Activa
            });

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _handler.Handle(new CancelSavingsAccountCommand("100000002", "cajero-1"), CancellationToken.None));
        }

        [Fact]
        public async Task ConSaldo_TransfiereFondosYCancela()
        {
            SeedSecondaryAccount(balance: 500m);

            await _handler.Handle(new CancelSavingsAccountCommand("100000002", "cajero-1"), CancellationToken.None);

            var secondary = _uow.Repo<CuentaAhorro>().Items.Single(a => a.NumeroCuenta == "100000002");
            var principal = _uow.Repo<CuentaAhorro>().Items.Single(a => a.NumeroCuenta == "100000001");

            Assert.Equal(0m, secondary.Balance);
            Assert.Equal(EstadoCuenta.Cancelada, secondary.Estado);
            Assert.Equal(1500m, principal.Balance);

            var txs = _uow.Repo<Transaccion>().Items;
            Assert.Equal(2, txs.Count);
            Assert.All(txs, t => Assert.Equal(TipoOperacion.TransferenciaEntreCuentasPropias, t.TipoOperacion));
            Assert.All(txs, t => Assert.NotNull(t.GrupoOperacionId));
            Assert.Single(txs, t => t.Tipo == TipoTransaccion.Debito && t.CuentaAhorroId == 2);
            Assert.Single(txs, t => t.Tipo == TipoTransaccion.Credito && t.CuentaAhorroId == 1);
        }

        [Fact]
        public async Task SinSaldo_CancelaSinTransacciones()
        {
            SeedSecondaryAccount(balance: 0m);

            await _handler.Handle(new CancelSavingsAccountCommand("100000002", "cajero-1"), CancellationToken.None);

            Assert.Equal(EstadoCuenta.Cancelada, _uow.Repo<CuentaAhorro>().Items.Single(a => a.NumeroCuenta == "100000002").Estado);
            Assert.Empty(_uow.Repo<Transaccion>().Items);
            Assert.Equal(1000m, _uow.Repo<CuentaAhorro>().Items.Single(a => a.NumeroCuenta == "100000001").Balance);
        }
    }
}