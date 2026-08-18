using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.CreditCards.CancelCreditCard;
using ArtemisBankingPro.Core.Application.CreditCards.CreateCreditCard;
using ArtemisBankingPro.Core.Application.CreditCards.UpdateCreditCardLimit;
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
    public class CreateCreditCardCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly Mock<IIdentityService> _identity = new();
        private readonly Mock<ICreditCardService> _cardService = new();
        private readonly FakeDateTimeProvider _date = new();
        private readonly Mock<IEmailService> _email = new();

        private CreateCreditCardCommandHandler Handler()
            => new(_uow, _identity.Object, _cardService.Object, _date, _email.Object, NullLogger<CreateCreditCardCommandHandler>.Instance);

        [Fact]
        public async Task ClienteInactivo_LanzaBusinessRule()
        {
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.InactiveClient());

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                Handler().Handle(new CreateCreditCardCommand("client-1", 15000m), CancellationToken.None));
        }

        [Fact]
        public async Task NoSePuedeGenerarNumero_LanzaConflict()
        {
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.ActiveClient());
            _cardService.Setup(x => x.GenerateUniqueCardNumberAsync())
                .ThrowsAsync(new InvalidOperationException("no único"));

            await Assert.ThrowsAsync<ConflictException>(() =>
                Handler().Handle(new CreateCreditCardCommand("client-1", 15000m), CancellationToken.None));
        }

        [Fact]
        public async Task ClienteActivo_CreaTarjetaEnmascarada()
        {
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.ActiveClient());
            _cardService.Setup(x => x.GenerateUniqueCardNumberAsync()).ReturnsAsync("1234567890123456");
            _cardService.Setup(x => x.HashCvc(It.IsAny<string>())).Returns("HASH123");

            var result = await Handler().Handle(new CreateCreditCardCommand("client-1", 15000m), CancellationToken.None);

            var card = Assert.Single(_uow.Repo<TarjetaCredito>().Items);
            Assert.Equal("1234567890123456", card.NumeroTarjeta);
            Assert.Equal("HASH123", card.CvcHash);
            Assert.Equal(15000m, card.LimiteCredito);
            Assert.Equal(0m, card.MontoAdeudado);
            Assert.Equal(EstadoTarjeta.Activa, card.Estado);
            Assert.Equal(_date.UtcNow.AddYears(3), card.FechaExpiracion);

            Assert.Equal("************3456", result.MaskedCardNumber);
            Assert.Equal("3456", result.LastFourDigits);
            Assert.Equal(15000m, result.CreditLimit);
            Assert.Equal(15000m, result.AvailableCredit);
            _email.Verify(x => x.SendCardAssignmentEmailAsync("juan.perez@example.com", "Juan Perez", "3456"), Times.Once);
        }
    }

    public class UpdateCreditCardLimitCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly Mock<IIdentityService> _identity = new();
        private readonly Mock<IEmailService> _email = new();

        private UpdateCreditCardLimitCommandHandler Handler()
            => new(_uow, _identity.Object, _email.Object, NullLogger<UpdateCreditCardLimitCommandHandler>.Instance);

        private void SeedActiveCard(decimal limit = 15000m, decimal debt = 3000m)
        {
            _uow.Repo<TarjetaCredito>().Items.Add(new TarjetaCredito
            {
                Id = 1,
                NumeroTarjeta = "1234567890123456",
                CvcHash = "hash",
                ClienteId = "client-1",
                LimiteCredito = limit,
                MontoAdeudado = debt,
                FechaExpiracion = DateTime.UtcNow.AddYears(3),
                Estado = EstadoTarjeta.Activa,
                FechaCreacion = DateTime.UtcNow
            });
        }

        [Fact]
        public async Task TarjetaNoExiste_LanzaNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() =>
                Handler().Handle(new UpdateCreditCardLimitCommand(999, 20000m), CancellationToken.None));
        }

        [Fact]
        public async Task TarjetaCancelada_LanzaBusinessRule()
        {
            _uow.Repo<TarjetaCredito>().Items.Add(new TarjetaCredito
            {
                Id = 1,
                NumeroTarjeta = "1234567890123456",
                CvcHash = "hash",
                ClienteId = "client-1",
                LimiteCredito = 15000m,
                MontoAdeudado = 0m,
                Estado = EstadoTarjeta.Cancelada
            });

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                Handler().Handle(new UpdateCreditCardLimitCommand(1, 20000m), CancellationToken.None));
        }

        [Fact]
        public async Task LimiteMenorQueDeuda_LanzaBusinessRule()
        {
            SeedActiveCard(debt: 3000m);

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                Handler().Handle(new UpdateCreditCardLimitCommand(1, 1000m), CancellationToken.None));
        }

        [Fact]
        public async Task LimiteValido_ActualizaLimiteYEnviaEmail()
        {
            SeedActiveCard();
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.ActiveClient());

            await Handler().Handle(new UpdateCreditCardLimitCommand(1, 25000m), CancellationToken.None);

            Assert.Equal(25000m, _uow.Repo<TarjetaCredito>().Items.Single().LimiteCredito);
            _email.Verify(x => x.SendCardLimitChangeEmailAsync("juan.perez@example.com", "Juan Perez", "3456", 25000m), Times.Once);
        }
    }

    public class CancelCreditCardCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly CancelCreditCardCommandHandler _handler;

        public CancelCreditCardCommandHandlerTests()
        {
            _handler = new CancelCreditCardCommandHandler(_uow);
        }

        [Fact]
        public async Task TarjetaNoExiste_LanzaNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(new CancelCreditCardCommand(999), CancellationToken.None));
        }

        [Fact]
        public async Task TarjetaYaCancelada_LanzaBusinessRule()
        {
            _uow.Repo<TarjetaCredito>().Items.Add(new TarjetaCredito
            {
                Id = 1,
                NumeroTarjeta = "1234567890123456",
                CvcHash = "hash",
                ClienteId = "client-1",
                LimiteCredito = 15000m,
                MontoAdeudado = 0m,
                Estado = EstadoTarjeta.Cancelada
            });

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _handler.Handle(new CancelCreditCardCommand(1), CancellationToken.None));
        }

        [Fact]
        public async Task TarjetaConDeuda_LanzaBusinessRule()
        {
            _uow.Repo<TarjetaCredito>().Items.Add(new TarjetaCredito
            {
                Id = 1,
                NumeroTarjeta = "1234567890123456",
                CvcHash = "hash",
                ClienteId = "client-1",
                LimiteCredito = 15000m,
                MontoAdeudado = 500m,
                Estado = EstadoTarjeta.Activa
            });

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _handler.Handle(new CancelCreditCardCommand(1), CancellationToken.None));
        }

        [Fact]
        public async Task TarjetaSinDeuda_CancelaLaTarjeta()
        {
            _uow.Repo<TarjetaCredito>().Items.Add(new TarjetaCredito
            {
                Id = 1,
                NumeroTarjeta = "1234567890123456",
                CvcHash = "hash",
                ClienteId = "client-1",
                LimiteCredito = 15000m,
                MontoAdeudado = 0m,
                Estado = EstadoTarjeta.Activa
            });

            await _handler.Handle(new CancelCreditCardCommand(1), CancellationToken.None);

            Assert.Equal(EstadoTarjeta.Cancelada, _uow.Repo<TarjetaCredito>().Items.Single().Estado);
        }
    }
}