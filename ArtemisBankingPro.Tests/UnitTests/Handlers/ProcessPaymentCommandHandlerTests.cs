using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.HermesPay.ProcessPayment;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ArtemisBankingPro.Tests.UnitTests.Handlers
{
    public class ProcessPaymentCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly Mock<ICreditCardService> _cardService = new();
        private readonly Mock<IIdentityService> _identity = new();
        private readonly FakeDateTimeProvider _date = new();
        private readonly Mock<IEmailService> _email = new();

        private ProcessPaymentCommandHandler Handler()
            => new(_uow, _cardService.Object, _identity.Object, _date, _email.Object, NullLogger<ProcessPaymentCommandHandler>.Instance);

        private void SeedCard(decimal limit = 10000m, decimal debt = 0m, string cvcHash = "HASH123", DateTime? expiration = null, EstadoTarjeta estado = EstadoTarjeta.Activa)
        {
            _uow.Repo<TarjetaCredito>().Items.Add(new TarjetaCredito
            {
                Id = 1,
                NumeroTarjeta = "1234567890123456",
                CvcHash = cvcHash,
                ClienteId = "client-1",
                LimiteCredito = limit,
                MontoAdeudado = debt,
                FechaExpiracion = expiration ?? new DateTime(2029, 12, 31),
                Estado = estado,
                FechaCreacion = _date.UtcNow
            });
        }

        private void SeedActiveCommerce()
        {
            _uow.Repo<Comercio>().Items.Add(new Comercio
            {
                Id = 1,
                Name = "Supermercado",
                Email = "sup@test.com",
                PhoneNumber = "8095551234",
                Rnc = "130123456",
                IsActive = true,
                ApplicationUserId = "commerce-user-1"
            });
            _uow.Repo<CuentaAhorro>().Items.Add(new CuentaAhorro
            {
                Id = 10,
                NumeroCuenta = "200000001",
                ClienteId = "commerce-user-1",
                Balance = 1000m,
                TipoCuenta = TipoCuenta.Principal,
                Estado = EstadoCuenta.Activa
            });
        }

        private static ProcessPaymentCommand Command(decimal amount = 2500m, string cvc = "123")
            => new(1, "1234567890123456", "12", "2029", cvc, amount);

        private void SetupDefaults()
        {
            _cardService.Setup(x => x.HashCvc("123")).Returns("HASH123");
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.ActiveClient());
        }

        [Fact]
        public async Task TarjetaNoExiste_LanzaBusinessRule()
        {
            SetupDefaults();
            SeedActiveCommerce();

            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
                Handler().Handle(Command(), CancellationToken.None));
            Assert.Contains("no existe", ex.Message);
        }

        [Fact]
        public async Task TarjetaCancelada_LanzaBusinessRule()
        {
            SetupDefaults();
            SeedCard(estado: EstadoTarjeta.Cancelada);
            SeedActiveCommerce();

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task CvcIncorrecto_LanzaBusinessRule()
        {
            SetupDefaults();
            SeedCard(cvcHash: "HASH123");

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                Handler().Handle(Command(cvc: "999"), CancellationToken.None));
        }

        [Fact]
        public async Task ExpiracionIncorrecta_LanzaBusinessRule()
        {
            SetupDefaults();
            SeedCard();

            var cmd = new ProcessPaymentCommand(1, "1234567890123456", "01", "2030", "123", 2500m);
            await Assert.ThrowsAsync<BusinessRuleException>(() => Handler().Handle(cmd, CancellationToken.None));
        }

        [Fact]
        public async Task TarjetaVencida_LanzaBusinessRule()
        {
            SetupDefaults();
            SeedCard(expiration: new DateTime(2025, 1, 1));

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task ComercioNoExiste_LanzaNotFound()
        {
            SetupDefaults();
            SeedCard();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task ComercioInactivo_LanzaBusinessRule()
        {
            SetupDefaults();
            SeedCard();
            _uow.Repo<Comercio>().Items.Add(new Comercio
            {
                Id = 1,
                Name = "Supermercado",
                Email = "sup@test.com",
                PhoneNumber = "8095551234",
                Rnc = "130123456",
                IsActive = false,
                ApplicationUserId = "commerce-user-1"
            });

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task ComercioSinUsuario_LanzaBusinessRule()
        {
            SetupDefaults();
            SeedCard();
            _uow.Repo<Comercio>().Items.Add(new Comercio
            {
                Id = 1,
                Name = "Supermercado",
                Email = "sup@test.com",
                PhoneNumber = "8095551234",
                Rnc = "130123456",
                IsActive = true
            });

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task ComercioSinCuentaPrincipalActiva_LanzaBusinessRule()
        {
            SetupDefaults();
            SeedCard();
            _uow.Repo<Comercio>().Items.Add(new Comercio
            {
                Id = 1,
                Name = "Supermercado",
                Email = "sup@test.com",
                PhoneNumber = "8095551234",
                Rnc = "130123456",
                IsActive = true,
                ApplicationUserId = "commerce-user-1"
            });

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                Handler().Handle(Command(), CancellationToken.None));
        }

        [Fact]
        public async Task CreditoInsuficiente_RechazaYPersisteConsumoRechazado()
        {
            SetupDefaults();
            SeedCard(limit: 1000m, debt: 0m);
            SeedActiveCommerce();

            var result = await Handler().Handle(Command(amount: 2500m), CancellationToken.None);

            Assert.False(result.Approved);
            Assert.Contains("excede", result.Message);

            var consumption = Assert.Single(_uow.Repo<Transaccion>().Items);
            Assert.Equal(EstadoTransaccion.Rechazada, consumption.Estado);
            Assert.Equal(TipoOperacion.PagoHermesPay, consumption.TipoOperacion);
            Assert.Equal(TipoTransaccion.Debito, consumption.Tipo);
            Assert.Equal(2500m, consumption.Monto);
            Assert.NotNull(consumption.MotivoRechazo);

            Assert.Equal(0m, _uow.Repo<TarjetaCredito>().Items.Single().MontoAdeudado);
            Assert.Equal(1000m, _uow.Repo<CuentaAhorro>().Items.Single().Balance);
        }

        [Fact]
        public async Task PagoAprobado_ActualizaDeudaSaldoYCreaTransacciones()
        {
            SetupDefaults();
            SeedCard(limit: 10000m, debt: 0m);
            SeedActiveCommerce();

            var result = await Handler().Handle(Command(amount: 2500m), CancellationToken.None);

            Assert.True(result.Approved);

            var card = _uow.Repo<TarjetaCredito>().Items.Single();
            Assert.Equal(2500m, card.MontoAdeudado);

            var commerceAccount = _uow.Repo<CuentaAhorro>().Items.Single();
            Assert.Equal(3500m, commerceAccount.Balance);

            var txs = _uow.Repo<Transaccion>().Items;
            Assert.Equal(2, txs.Count);
            Assert.Single(txs, t => t.Tipo == TipoTransaccion.Debito && t.TarjetaCreditoId == 1 && t.Estado == EstadoTransaccion.Aprobada);
            Assert.Single(txs, t => t.Tipo == TipoTransaccion.Credito && t.CuentaAhorroId == 10 && t.Estado == EstadoTransaccion.Aprobada);
            Assert.Equal(txs[0].GrupoOperacionId, txs[1].GrupoOperacionId);

            _email.Verify(x => x.SendCardConsumptionEmailAsync(
                "juan.perez@example.com", "Juan Perez", 2500m, "3456", "Supermercado", It.IsAny<DateTime>()), Times.Once);
            _email.Verify(x => x.SendPaymentReceivedEmailAsync(
                "sup@test.com", "Supermercado", 2500m, "3456", It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task PagoAprobado_SinPropietario_SigueNotificandoAlComercio()
        {
            SetupDefaults();
            SeedCard(limit: 10000m, debt: 0m);
            SeedActiveCommerce();
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync((ArtemisBankingPro.Core.Application.Users.GetUserById.UserDetailDto?)null);

            var result = await Handler().Handle(Command(amount: 2500m), CancellationToken.None);

            Assert.True(result.Approved);
            _email.Verify(x => x.SendPaymentReceivedEmailAsync(
                "sup@test.com", "Supermercado", 2500m, "3456", It.IsAny<DateTime>()), Times.Once);
        }
    }
}