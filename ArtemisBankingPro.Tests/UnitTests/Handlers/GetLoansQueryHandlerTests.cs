using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Application.Loans.GetLoans;
using ArtemisBankingPro.Core.Application.Users.GetUserById;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Tests.Helpers;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ArtemisBankingPro.Tests.UnitTests.Handlers
{
    public class GetLoansQueryHandlerTests
    {
        private readonly Mock<IIdentityService> _identity = new();
        private readonly FakeUnitOfWork _uow = new();
        private readonly GetLoansQueryHandler _handler;

        public GetLoansQueryHandlerTests()
        {
            _handler = new GetLoansQueryHandler(_uow, _identity.Object);
        }

        private void SeedLoans()
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
            _uow.Repo<Prestamo>().Items.Add(new Prestamo
            {
                Id = 2,
                NumeroPrestamo = "200000002",
                ClienteId = "client-2",
                CapitalAprobado = 50000m,
                TasaInteresAnual = 10m,
                PlazoMeses = 24,
                Estado = EstadoPrestamo.Completado,
                FechaCreacion = new DateTime(2025, 6, 1)
            });

            foreach (var cuota in TestData.AmortizationCuotas(1, 12, 8884.88m))
                _uow.Repo<CuotaAmortizacion>().Items.Add(cuota);
            foreach (var cuota in TestData.AmortizationCuotas(2, 24, 2307m))
                _uow.Repo<CuotaAmortizacion>().Items.Add(cuota);

            _identity.Setup(x => x.GetUserDetailByIdAsync("client-1")).ReturnsAsync(TestData.ActiveClient("client-1"));
            _identity.Setup(x => x.GetUserDetailByIdAsync("client-2")).ReturnsAsync(TestData.ActiveClient("client-2"));
        }

        [Fact]
        public async Task DevuelvePrestamosPaginados()
        {
            SeedLoans();

            var result = await _handler.Handle(new GetLoansQuery(Page: 1, PageSize: 20, Status: "todos"), CancellationToken.None);

            Assert.Equal(2, result.TotalRecords);
            Assert.Equal(2, result.Data.Count);
        }

        [Fact]
        public async Task FiltraPorEstadoActivos()
        {
            SeedLoans();

            var result = await _handler.Handle(new GetLoansQuery(Status: "activos"), CancellationToken.None);

            var loan = Assert.Single(result.Data);
            Assert.Equal("200000001", loan.LoanNumber);
            Assert.Equal(EstadoPrestamo.Activo.ToString(), loan.Status);
        }

        [Fact]
        public async Task FiltraPorIdentificacion()
        {
            SeedLoans();
            _identity.Setup(x => x.GetUserIdByCedulaAsync("00123456789")).ReturnsAsync("client-1");

            var result = await _handler.Handle(new GetLoansQuery(Identification: "00123456789"), CancellationToken.None);

            var loan = Assert.Single(result.Data);
            Assert.Equal("client-1", loan.ClientId);
        }

        [Fact]
        public async Task CalculaCuotasPagadasYPendientes()
        {
            SeedLoans();

            var result = await _handler.Handle(new GetLoansQuery(Identification: null, Status: "activos"), CancellationToken.None);

            var loan = result.Data.Single();
            Assert.Equal(0, loan.PaidInstallments);
            Assert.Equal(12m * 8884.88m, loan.PendingAmount);
            Assert.Equal("Al día", loan.ClientPaymentStatus);
        }
    }
}