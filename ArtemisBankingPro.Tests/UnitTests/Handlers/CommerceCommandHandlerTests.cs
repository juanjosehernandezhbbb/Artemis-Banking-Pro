using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Commerces.ChangeCommerceStatus;
using ArtemisBankingPro.Core.Application.Commerces.CreateCommerce;
using ArtemisBankingPro.Core.Application.Commerces.UpdateCommerce;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Tests.Helpers;
using Moq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ArtemisBankingPro.Tests.UnitTests.Handlers
{
    public class CreateCommerceCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly FakeDateTimeProvider _date = new();
        private readonly CreateCommerceCommandHandler _handler;

        public CreateCommerceCommandHandlerTests()
        {
            _handler = new CreateCommerceCommandHandler(_uow, _date);
        }

        [Fact]
        public async Task RncDuplicado_LanzaConflict()
        {
            _uow.Repo<Comercio>().Items.Add(new Comercio
            {
                Id = 1,
                Name = "Otro",
                Email = "otro@test.com",
                PhoneNumber = "8090000000",
                Rnc = "130123456"
            });

            var cmd = new CreateCommerceCommand("Supermercado", null, "sup@test.com", "8095551234", "130123456");
            await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Fact]
        public async Task EmailDuplicado_LanzaConflict()
        {
            _uow.Repo<Comercio>().Items.Add(new Comercio
            {
                Id = 1,
                Name = "Otro",
                Email = "sup@test.com",
                PhoneNumber = "8090000000",
                Rnc = "999999999"
            });

            var cmd = new CreateCommerceCommand("Supermercado", null, "sup@test.com", "8095551234", "130123456");
            await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Fact]
        public async Task ComercioValido_CreaComercioActivo()
        {
            var result = await _handler.Handle(
                new CreateCommerceCommand("Supermercado", "Tienda", "sup@test.com", "8095551234", "130123456", "admin-1"),
                CancellationToken.None);

            var comercio = Assert.Single(_uow.Repo<Comercio>().Items);
            Assert.True(comercio.IsActive);
            Assert.Equal("admin-1", comercio.CreatedByUserId);
            Assert.True(result.IsActive);
            Assert.Equal("Supermercado", result.Name);
        }
    }

    public class UpdateCommerceCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly UpdateCommerceCommandHandler _handler;

        public UpdateCommerceCommandHandlerTests()
        {
            _handler = new UpdateCommerceCommandHandler(_uow);
        }

        private void SeedCommerce(bool isActive = true)
        {
            _uow.Repo<Comercio>().Items.Add(new Comercio
            {
                Id = 1,
                Name = "Supermercado",
                Email = "sup@test.com",
                PhoneNumber = "8095551234",
                Rnc = "130123456",
                IsActive = isActive
            });
        }

        [Fact]
        public async Task ComercioNoExiste_LanzaNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(new UpdateCommerceCommand(999, "A", null, "a@b.com", "8090000000", "130000000"), CancellationToken.None));
        }

        [Fact]
        public async Task RncDeOtroComercio_LanzaConflict()
        {
            SeedCommerce();
            _uow.Repo<Comercio>().Items.Add(new Comercio
            {
                Id = 2,
                Name = "Otro",
                Email = "otro@test.com",
                PhoneNumber = "8090000000",
                Rnc = "130654321"
            });

            await Assert.ThrowsAsync<ConflictException>(() =>
                _handler.Handle(new UpdateCommerceCommand(1, "Supermercado", null, "sup@test.com", "8095551234", "130654321"), CancellationToken.None));
        }

        [Fact]
        public async Task ComercioValido_ActualizaDatosSinTocarIsActive()
        {
            SeedCommerce(isActive: false);

            await _handler.Handle(new UpdateCommerceCommand(1, "Hipermercado", "Nueva desc", "nuevo@test.com", "8099999999", "130123456"), CancellationToken.None);

            var comercio = _uow.Repo<Comercio>().Items.Single();
            Assert.Equal("Hipermercado", comercio.Name);
            Assert.Equal("Nueva desc", comercio.Description);
            Assert.Equal("nuevo@test.com", comercio.Email);
            Assert.Equal("8099999999", comercio.PhoneNumber);
            Assert.False(comercio.IsActive);
        }
    }

    public class ChangeCommerceStatusCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly Mock<IIdentityService> _identity = new();
        private readonly ChangeCommerceStatusCommandHandler _handler;

        public ChangeCommerceStatusCommandHandlerTests()
        {
            _handler = new ChangeCommerceStatusCommandHandler(_uow, _identity.Object);
        }

        [Fact]
        public async Task ComercioNoExiste_LanzaNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(new ChangeCommerceStatusCommand(999, false), CancellationToken.None));
        }

        [Fact]
        public async Task DesactivarComercioConUsuario_DesactivaAlUsuario()
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

            await _handler.Handle(new ChangeCommerceStatusCommand(1, false), CancellationToken.None);

            Assert.False(_uow.Repo<Comercio>().Items.Single().IsActive);
            _identity.Verify(x => x.SetUserActiveStatusAsync("commerce-user-1", false), Times.Once);
        }

        [Fact]
        public async Task DesactivarComercioSinUsuario_NoLlamaAlServicioDeIdentidad()
        {
            _uow.Repo<Comercio>().Items.Add(new Comercio
            {
                Id = 1,
                Name = "Supermercado",
                Email = "sup@test.com",
                PhoneNumber = "8095551234",
                Rnc = "130123456",
                IsActive = true
            });

            await _handler.Handle(new ChangeCommerceStatusCommand(1, false), CancellationToken.None);

            _identity.Verify(x => x.SetUserActiveStatusAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task ReactivarComercio_NoDesactivaAlUsuario()
        {
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

            await _handler.Handle(new ChangeCommerceStatusCommand(1, true), CancellationToken.None);

            Assert.True(_uow.Repo<Comercio>().Items.Single().IsActive);
            _identity.Verify(x => x.SetUserActiveStatusAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }
    }
}