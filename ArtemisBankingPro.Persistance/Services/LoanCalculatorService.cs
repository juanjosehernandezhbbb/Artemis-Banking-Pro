using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Infrastructure.Services
{
    public class LoanCalculatorService : ILoanCalculatorService
    {
        public (decimal MonthlyInstallment, decimal TotalAmountToPay) CalculateFrenchAmortization(
            decimal capital, decimal annualInterestRate, int termInMonths)
        {
            var monthlyRate = (annualInterestRate / 100m) / 12m;

            if (monthlyRate <= 0)
            {
                var simpleInstallment = capital / termInMonths;
                return (Math.Round(simpleInstallment, 2, MidpointRounding.AwayFromZero), capital);
            }

            var factor = (decimal)Math.Pow((double)(1m + monthlyRate), -termInMonths);
            var installment = capital * (monthlyRate / (1m - factor));

            var roundedInstallment = Math.Round(installment, 2, MidpointRounding.AwayFromZero);
            var total = Math.Round(installment * termInMonths, 2, MidpointRounding.AwayFromZero);

            return (roundedInstallment, total);
        }

        public List<CuotaAmortizacion> GenerateAmortizationSchedule(
            int prestamoId, decimal capital, decimal annualInterestRate, int termInMonths)
        {
            var monthlyRate = (annualInterestRate / 100m) / 12m;
            var (installment, _) = CalculateFrenchAmortization(capital, annualInterestRate, termInMonths);

            var cuotas = new List<CuotaAmortizacion>();
            var remaining = capital;
            var start = DateTime.UtcNow;

            for (int i = 1; i <= termInMonths; i++)
            {
                var interest = remaining * monthlyRate;
                var capitalPart = installment - interest;
                remaining -= capitalPart;

                cuotas.Add(new CuotaAmortizacion
                {
                    PrestamoId = prestamoId,
                    NumeroCuota = i,
                    FechaVencimiento = start.AddMonths(i),
                    FechaPago = null,
                    ValorCuota = Math.Round(installment, 2, MidpointRounding.AwayFromZero),
                    MontoInteres = Math.Round(interest, 2, MidpointRounding.AwayFromZero),
                    MontoCapital = Math.Round(capitalPart, 2, MidpointRounding.AwayFromZero),
                    SaldoPendiente = Math.Round(remaining, 2, MidpointRounding.AwayFromZero),
                    EstadoPago = EstadoCuota.Pendiente,
                    Atrasada = false
                });
            }

            return cuotas;
        }
    }
}