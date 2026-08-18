using ArtemisBankingPro.Core.Domain.Entities;
using System.Collections.Generic;

namespace ArtemisBankingPro.Core.Application.Common.Interfaces
{
    public interface ILoanCalculatorService
    {
        (decimal MonthlyInstallment, decimal TotalAmountToPay) CalculateFrenchAmortization(decimal capital, decimal annualInterestRate, int termInMonths);

        List<CuotaAmortizacion> GenerateAmortizationSchedule(int prestamoId, decimal capital, decimal annualInterestRate, int termInMonths);
    }
}