using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Infrastructure.Services;
using System;
using System.Linq;
using Xunit;

namespace ArtemisBankingPro.Tests.UnitTests
{
    public class LoanCalculatorServiceTests
    {
        private readonly LoanCalculatorService _service = new();

        [Fact]
        public void CalculateFrenchAmortization_ConValoresConocidos_DevuelveCuotaCorrecta()
        {
            var (installment, total) = _service.CalculateFrenchAmortization(100000m, 12m, 12);

            Assert.Equal(8884.88m, installment);
            Assert.Equal(106618.55m, total);
        }

        [Fact]
        public void CalculateFrenchAmortization_SegundoCaso_DevuelveCuotaCorrecta()
        {
            var (installment, total) = _service.CalculateFrenchAmortization(120000m, 24m, 36);

            Assert.Equal(4707.94m, installment);
            Assert.Equal(169485.92m, total);
        }

        [Fact]
        public void CalculateFrenchAmortization_ConTasaCero_DevuelveCapitalEntrePlazo()
        {
            var (installment, total) = _service.CalculateFrenchAmortization(5000m, 0m, 10);

            Assert.Equal(500m, installment);
            Assert.Equal(5000m, total);
        }

        [Fact]
        public void CalculateFrenchAmortization_TotalEsAlMenosCapital()
        {
            var (_, total) = _service.CalculateFrenchAmortization(100000m, 12m, 12);
            Assert.True(total >= 100000m);
        }

        [Fact]
        public void GenerateAmortizationSchedule_GeneraUnaCuotaPorMes()
        {
            var cuotas = _service.GenerateAmortizationSchedule(1, 100000m, 12m, 12);

            Assert.Equal(12, cuotas.Count);
            Assert.All(cuotas, c => Assert.Equal(1, c.PrestamoId));
        }

        [Fact]
        public void GenerateAmortizationSchedule_UltimaCuota_DejaSaldoCero()
        {
            var cuotas = _service.GenerateAmortizationSchedule(1, 100000m, 12m, 12);

            var last = cuotas[^1];
            Assert.True(Math.Abs(last.SaldoPendiente) <= 0.02m, $"Saldo final inesperado: {last.SaldoPendiente}");
        }

        [Fact]
        public void GenerateAmortizationSchedule_SumaDelCapital_IgualaElCapitalAprobado()
        {
            var cuotas = _service.GenerateAmortizationSchedule(1, 100000m, 12m, 12);

            var totalCapital = cuotas.Sum(c => c.MontoCapital);
            Assert.True(Math.Abs(totalCapital - 100000m) <= 0.05m, $"Total de capital inesperado: {totalCapital}");
        }

        [Fact]
        public void GenerateAmortizationSchedule_InteresEsDecreciente()
        {
            var cuotas = _service.GenerateAmortizationSchedule(1, 100000m, 12m, 12);

            for (int i = 1; i < cuotas.Count; i++)
            {
                Assert.True(cuotas[i].MontoInteres <= cuotas[i - 1].MontoInteres);
            }
        }

        [Fact]
        public void GenerateAmortizationSchedule_CuotasEstanPendientesYNoAtrasadas()
        {
            var cuotas = _service.GenerateAmortizationSchedule(1, 100000m, 12m, 12);

            Assert.All(cuotas, c =>
            {
                Assert.Equal(EstadoCuota.Pendiente, c.EstadoPago);
                Assert.False(c.Atrasada);
                Assert.Null(c.FechaPago);
            });
        }

        [Fact]
        public void GenerateAmortizationSchedule_FechasDeVencimiento_SonProgresivas()
        {
            var cuotas = _service.GenerateAmortizationSchedule(1, 100000m, 12m, 12);

            for (int i = 1; i < cuotas.Count; i++)
            {
                Assert.True(cuotas[i].FechaVencimiento > cuotas[i - 1].FechaVencimiento);
            }
        }
    }
}