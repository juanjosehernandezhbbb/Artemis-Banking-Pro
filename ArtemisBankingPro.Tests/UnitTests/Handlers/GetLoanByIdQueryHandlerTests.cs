using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Loans.GetLoanById;
using ArtemisBankingPro.Core.Application.Users.GetUserById;
using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Infrastructure.Services;
using ArtemisBankingPro.Tests.Helpers;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ArtemisBankingPro.Tests.UnitTests.Handlers
{
    public class GetLoanByIdQueryHandlerTests
    {
        private readonly Mock<IIdentityService> _identity = new();
        private readonly FakeUnitOfWork _uow = new();
        private readonly GetLoanByIdQueryHandler _handler;

        public GetLoanByIdQueryHandlerTests()
        {
            _handler = new GetLoanByIdQueryHandler(_uow, _identity.Object, new LoanCalculatorService());
        }

        [Fact]
        public async Task PrestamoNoExiste_LanzaNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(new GetLoanByIdQuery(999), CancellationToken.None));
        }

        [Fact]
        public async Task DevuelveDetalleConAmortizacion()
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
            foreach (var cuota in TestData.AmortizationCuotas(1, 12, 8884.88m))
                _uow.Repo<CuotaAmortizacion>().Items.Add(cuota);

            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.ActiveClient("client-1"));

            var result = await _handler.Handle(new GetLoanByIdQuery(1), CancellationToken.None);

            Assert.Equal("200000001", result.LoanNumber);
            Assert.Equal("Juan Perez", result.ClientFullName);
            Assert.Equal(12, result.Amortization.Count);
            Assert.Equal(8884.88m, result.MonthlyInstallment);
            Assert.Equal(12m * 8884.88m, result.PendingAmount);
            Assert.Equal(1, result.Amortization.First().InstallmentNumber);
            Assert.Equal(EstadoCuota.Pendiente.ToString(), result.Amortization.First().PaymentStatus);
        }
    }
}