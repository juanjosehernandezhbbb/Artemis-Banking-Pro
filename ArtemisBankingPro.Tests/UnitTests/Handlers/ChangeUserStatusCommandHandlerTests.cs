using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Users.ChangeUserStatus;
using ArtemisBankingPro.Core.Application.Users.GetUserById;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Tests.Helpers;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ArtemisBankingPro.Tests.UnitTests.Handlers
{
    public class ChangeUserStatusCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identity = new();
        private readonly ChangeUserStatusCommandHandler _handler;

        public ChangeUserStatusCommandHandlerTests()
        {
            _handler = new ChangeUserStatusCommandHandler(_identity.Object);
        }

        [Fact]
        public async Task UsuarioNoExiste_LanzaNotFound()
        {
            _identity.Setup(x => x.GetUserDetailByIdAsync("user-1")).ReturnsAsync((UserDetailDto?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(new ChangeUserStatusCommand("user-1", true, "admin-1"), CancellationToken.None));
        }

        [Fact]
        public async Task ModificarSuPropioEstado_LanzaForbidden()
        {
            _identity.Setup(x => x.GetUserDetailByIdAsync("admin-1"))
                .ReturnsAsync(TestData.ActiveClient("admin-1"));

            await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                _handler.Handle(new ChangeUserStatusCommand("admin-1", true, "admin-1"), CancellationToken.None));
        }

        [Fact]
        public async Task EstadoValido_ActualizaElEstadoDelUsuario()
        {
            _identity.Setup(x => x.GetUserDetailByIdAsync("user-1")).ReturnsAsync(TestData.ActiveClient("user-1"));

            await _handler.Handle(new ChangeUserStatusCommand("user-1", false, "admin-1"), CancellationToken.None);

            _identity.Verify(x => x.SetUserActiveStatusAsync("user-1", false), Times.Once);
        }
    }
}