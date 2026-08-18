using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Loans.UpdateLoanRate;
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
    public class UpdateLoanRateCommandHandlerTests
    {
        private readonly FakeUnitOfWork _uow = new();
        private readonly Mock<IIdentityService> _identity = new();
        private readonly Mock<ILoanCalculatorService> _calc = new();
        private readonly Mock<IEmailService> _email = new();
        private readonly UpdateLoanRateCommandHandler _handler;

        public UpdateLoanRateCommandHandlerTests()
        {
            _handler = new UpdateLoanRateCommandHandler(_uow, _identity.Object, _calc.Object, _email.Object, NullLogger<UpdateLoanRateCommandHandler>.Instance);
        }

        private void SeedActiveLoan()
        {
            _uow.Repo<Prestamo>().Items.Add(new Prestamo
            {
                Id = 1,
                NumeroPrestamo = "200000001",
                ClienteId = "client-1",
                CapitalAprobado = 100000m,
                TasaInteresAnual = 12m,
                PlazoMeses = 12,
                Estado = EstadoPrestamo.Activo,
                FechaCreacion = new DateTime(2026, 1, 1)
            });
        }

        [Fact]
        public async Task PrestamoNoExiste_LanzaNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(new UpdateLoanRateCommand(999, 10m), CancellationToken.None));
        }

        [Fact]
        public async Task PrestamoCompletado_LanzaBusinessRule()
        {
            _uow.Repo<Prestamo>().Items.Add(new Prestamo
            {
                Id = 1,
                NumeroPrestamo = "200000001",
                ClienteId = "client-1",
                Estado = EstadoPrestamo.Completado
            });

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _handler.Handle(new UpdateLoanRateCommand(1, 10m), CancellationToken.None));
        }

        [Fact]
        public async Task SinCuotasFuturasPendientes_LanzaBusinessRule()
        {
            SeedActiveLoan();
            _uow.Repo<CuotaAmortizacion>().Items.AddRange(TestData.AmortizationCuotas(1, 12, 8884.88m, future: true, paid: true));

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _handler.Handle(new UpdateLoanRateCommand(1, 10m), CancellationToken.None));
        }

        [Fact]
        public async Task TasaValida_RecalculaCuotasFuturasYActualizaTasa()
        {
            SeedActiveLoan();
            foreach (var cuota in TestData.AmortizationCuotas(1, 12, 8884.88m, future: true))
                _uow.Repo<CuotaAmortizacion>().Items.Add(cuota);

            var remainingCapital = _uow.Repo<CuotaAmortizacion>().Items.Where(c => c.PrestamoId == 1).Sum(c => c.MontoCapital);
            _calc.Setup(x => x.CalculateFrenchAmortization(remainingCapital, 10m, 12)).Returns((900m, 10800m));
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.ActiveClient("client-1"));

            await _handler.Handle(new UpdateLoanRateCommand(1, 10m), CancellationToken.None);

            var prestamo = _uow.Repo<Prestamo>().Items.Single();
            Assert.Equal(10m, prestamo.TasaInteresAnual);

            Assert.All(_uow.Repo<CuotaAmortizacion>().Items, c => Assert.Equal(900m, c.ValorCuota));
            _email.Verify(x => x.SendLoanRateChangeEmailAsync("juan.perez@example.com", "Juan Perez", "200000001", 10m), Times.Once);
        }
    }
}