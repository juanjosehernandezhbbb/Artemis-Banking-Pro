using ArtemisBankingPro.Core.Application.Account.Login;
using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using AutoMapper;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ArtemisBankingPro.Tests.UnitTests.Handlers
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identity = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _handler = new LoginCommandHandler(_identity.Object, _mapper.Object);
        }

        private static ApplicationUser User(bool activo = true)
            => new()
            {
                Id = "user-1",
                UserName = "cliente1",
                Nombre = "Juan",
                Apellido = "Perez",
                Cedula = "00112345678",
                Activo = activo
            };

        private void SetupMapping(ApplicationUser user)
        {
            _mapper
                .Setup(x => x.Map<LoginResultDto>(user))
                .Returns(new LoginResultDto(user.Id, user.UserName, user.Nombre, user.Apellido, Role: null!));
        }

        [Fact]
        public async Task CredencialesValidas_DevuelveLoginResultConRol()
        {
            var user = User();
            SetupMapping(user);
            _identity.Setup(x => x.ValidateCredentialsAsync("cliente1", "Pass")).ReturnsAsync((true, user));
            _identity.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { DomainConstants.RolCliente });

            var result = await _handler.Handle(new LoginCommand("cliente1", "Pass", new[] { DomainConstants.RolCliente }), CancellationToken.None);

            Assert.Equal("user-1", result.UserId);
            Assert.Equal("cliente1", result.UserName);
            Assert.Equal(DomainConstants.RolCliente, result.Role);
            Assert.Null(result.CommerceId);
        }

        [Fact]
        public async Task CredencialesInvalidas_LanzaUnauthorized()
        {
            _identity.Setup(x => x.ValidateCredentialsAsync("cliente1", "Wrong")).ReturnsAsync((false, (ApplicationUser?)null));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _handler.Handle(new LoginCommand("cliente1", "Wrong", new[] { DomainConstants.RolCliente }), CancellationToken.None));
        }

        [Fact]
        public async Task UsuarioInactivo_LanzaUnauthorized()
        {
            var user = User(activo: false);
            SetupMapping(user);
            _identity.Setup(x => x.ValidateCredentialsAsync("cliente1", "Pass")).ReturnsAsync((true, user));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _handler.Handle(new LoginCommand("cliente1", "Pass", new[] { DomainConstants.RolCliente }), CancellationToken.None));
        }

        [Fact]
        public async Task RolNoPermitido_LanzaForbidden()
        {
            var user = User();
            SetupMapping(user);
            _identity.Setup(x => x.ValidateCredentialsAsync("cliente1", "Pass")).ReturnsAsync((true, user));
            _identity.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { DomainConstants.RolCliente });

            await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                _handler.Handle(new LoginCommand("cliente1", "Pass", new[] { DomainConstants.RolAdministrador }), CancellationToken.None));
        }

        [Fact]
        public async Task UsuarioComercio_ResuelveCommerceId()
        {
            var user = User();
            SetupMapping(user);
            _identity.Setup(x => x.ValidateCredentialsAsync("comercio1", "Pass")).ReturnsAsync((true, user));
            _identity.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { DomainConstants.RolComercio });
            _identity.Setup(x => x.GetCommerceIdByUserIdAsync("user-1")).ReturnsAsync(7);

            var result = await _handler.Handle(new LoginCommand("comercio1", "Pass", new[] { DomainConstants.RolComercio }), CancellationToken.None);

            Assert.Equal(DomainConstants.RolComercio, result.Role);
            Assert.Equal(7, result.CommerceId);
        }
    }
}